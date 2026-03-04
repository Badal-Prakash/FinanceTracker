using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace FinanceTracker.Application.Reports;

// ─── Filter DTO ───────────────────────────────────────────────────────────────

public record ReportFilters(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Status = null,
    Guid? CategoryId = null,
    Guid? UserId = null);

// ─── Response DTOs (returned as JSON to the frontend) ─────────────────────────

public record ExpenseReportRow(
    string Title,
    string SubmittedBy,
    string Category,
    decimal Amount,
    DateTime ExpenseDate,
    string Status,
    string? Description,
    string? ReceiptUrl);

public record CategorySummaryRow(
    string CategoryName,
    string CategoryColor,
    int Count,
    decimal TotalAmount,
    decimal Percentage);

public record ExpenseReportDto(
    int TotalCount,
    decimal TotalAmount,
    int ApprovedCount,
    int PendingCount,
    List<CategorySummaryRow> CategorySummary,
    List<ExpenseReportRow> Rows);

// ─── Shared data loader ───────────────────────────────────────────────────────

public static class ReportDataLoader
{
    public static async Task<List<ExpenseReportRow>> LoadAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ReportFilters filters,
        CancellationToken ct)
    {
        var query = context.Expenses
            .Include(e => e.Category)
            .Include(e => e.SubmittedBy)
            .AsNoTracking()
            .AsQueryable();

        if (currentUser.Role == "Employee")
            query = query.Where(e => e.SubmittedById == currentUser.UserId);

        if (filters.FromDate.HasValue)
            query = query.Where(e => e.ExpenseDate >= filters.FromDate.Value);

        if (filters.ToDate.HasValue)
            query = query.Where(e => e.ExpenseDate <= filters.ToDate.Value);

        if (!string.IsNullOrEmpty(filters.Status) &&
            Enum.TryParse<ExpenseStatus>(filters.Status, out var statusEnum))
            query = query.Where(e => e.Status == statusEnum);

        if (filters.CategoryId.HasValue)
            query = query.Where(e => e.CategoryId == filters.CategoryId.Value);

        if (filters.UserId.HasValue && currentUser.Role != "Employee")
            query = query.Where(e => e.SubmittedById == filters.UserId.Value);

        return await query
            .OrderByDescending(e => e.ExpenseDate)
            .Select(e => new ExpenseReportRow(
                e.Title,
                e.SubmittedBy != null ? e.SubmittedBy.FullName : "Unknown",
                e.Category != null ? e.Category.Name : "Uncategorised",
                e.Amount,
                e.ExpenseDate,
                e.Status.ToString(),
                e.Description,
                e.ReceiptUrl))
            .ToListAsync(ct);
    }

    public static List<CategorySummaryRow> BuildCategorySummary(
        List<ExpenseReportRow> rows)
    {
        var total = rows.Sum(r => r.Amount);
        return rows
            .GroupBy(r => r.Category)
            .Select(g => new CategorySummaryRow(
                g.Key, "#6366f1",
                g.Count(),
                g.Sum(r => r.Amount),
                total > 0 ? Math.Round(g.Sum(r => r.Amount) / total * 100, 1) : 0))
            .OrderByDescending(c => c.TotalAmount)
            .ToList();
    }
}

// ─── QUERY: Get expense report data as JSON ───────────────────────────────────

public record GetExpenseReportQuery(ReportFilters Filters) : IRequest<ExpenseReportDto>;

public class GetExpenseReportQueryHandler
    : IRequestHandler<GetExpenseReportQuery, ExpenseReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetExpenseReportQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task<ExpenseReportDto> Handle(
        GetExpenseReportQuery request, CancellationToken ct)
    {
        var rows = await ReportDataLoader.LoadAsync(_context, _currentUser, request.Filters, ct);
        var summary = ReportDataLoader.BuildCategorySummary(rows);

        return new ExpenseReportDto(
            rows.Count,
            rows.Sum(r => r.Amount),
            rows.Count(r => r.Status == "Approved"),
            rows.Count(r => r.Status == "Submitted"),
            summary,
            rows);
    }
}

// ─── QUERY: Export expenses as CSV ───────────────────────────────────────────

public record ExportExpensesCsvQuery(ReportFilters Filters) : IRequest<byte[]>;

public class ExportExpensesCsvQueryHandler
    : IRequestHandler<ExportExpensesCsvQuery, byte[]>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ExportExpensesCsvQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task<byte[]> Handle(
        ExportExpensesCsvQuery request, CancellationToken ct)
    {
        var rows = await ReportDataLoader.LoadAsync(_context, _currentUser, request.Filters, ct);
        var sb = new StringBuilder();

        sb.AppendLine("Title,Submitted By,Category,Amount,Date,Status,Description,Receipt");

        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(r.Title),
                CsvEscape(r.SubmittedBy),
                CsvEscape(r.Category),
                r.Amount.ToString("F2", CultureInfo.InvariantCulture),
                r.ExpenseDate.ToString("yyyy-MM-dd"),
                r.Status,
                CsvEscape(r.Description ?? ""),
                CsvEscape(r.ReceiptUrl ?? "")));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}

// ─── QUERY: Export budget vs actual as CSV ────────────────────────────────────

public record ExportBudgetCsvQuery(int Month, int Year) : IRequest<byte[]>;

public class ExportBudgetCsvQueryHandler
    : IRequestHandler<ExportBudgetCsvQuery, byte[]>
{
    private readonly IApplicationDbContext _context;

    public ExportBudgetCsvQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<byte[]> Handle(
        ExportBudgetCsvQuery request, CancellationToken ct)
    {
        var budgets = await _context.Budgets
            .Include(b => b.Category)
            .Where(b => b.Month == request.Month && b.Year == request.Year)
            .AsNoTracking()
            .ToListAsync(ct);

        var start = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var spentByCat = await _context.Expenses
            .Where(e => e.Status == ExpenseStatus.Approved
                     && e.ExpenseDate >= start && e.ExpenseDate < end)
            .GroupBy(e => e.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(e => e.Amount) })
            .AsNoTracking()
            .ToDictionaryAsync(e => e.CategoryId, e => e.Total, ct);

        var sb = new StringBuilder();
        sb.AppendLine("Category,Budgeted,Spent,Remaining,Utilisation %");

        foreach (var b in budgets.OrderBy(b => b.Category?.Name))
        {
            var spent = spentByCat.GetValueOrDefault(b.CategoryId, 0m);
            var remaining = b.Amount - spent;
            var pct = b.Amount > 0
                ? Math.Round(spent / b.Amount * 100, 1) : 0m;

            sb.AppendLine(string.Join(",",
                b.Category?.Name ?? "Unknown",
                b.Amount.ToString("F2", CultureInfo.InvariantCulture),
                spent.ToString("F2", CultureInfo.InvariantCulture),
                remaining.ToString("F2", CultureInfo.InvariantCulture),
                pct.ToString("F1", CultureInfo.InvariantCulture) + "%"));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}