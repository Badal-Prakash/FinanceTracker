using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Expenses;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record ImportRowDto(
    int RowNumber,
    string Title,
    string Description,
    decimal Amount,
    string ExpenseDate,
    string Category,
    bool IsValid,
    string? Error);

public record ImportPreviewDto(
    List<ImportRowDto> Rows,
    int ValidCount,
    int ErrorCount,
    List<string> AvailableCategories);

public record ImportResultDto(
    int Imported,
    int Skipped,
    List<string> Errors);

// ─── SHARED PARSER ────────────────────────────────────────────────────────────

internal static class ExpenseImportParser
{
    internal static List<string[]> Parse(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return ext == ".xlsx" ? ParseExcel(file) : ParseCsv(file);
    }

    private static List<string[]> ParseCsv(IFormFile file)
    {
        var rows = new List<string[]>();
        using var reader = new StreamReader(file.OpenReadStream());
        reader.ReadLine(); // skip header row
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            rows.Add(SplitCsvLine(line));
        }
        return rows;
    }

    private static List<string[]> ParseExcel(IFormFile file)
    {
        // Requires NuGet: ClosedXML
        var rows = new List<string[]>();
        using var stream = file.OpenReadStream();
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var ws = workbook.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (int r = 2; r <= lastRow; r++)
        {
            var row = ws.Row(r);
            if (row.IsEmpty()) continue;
            rows.Add(new[]
            {
                row.Cell(1).GetString(),
                row.Cell(2).GetString(),
                row.Cell(3).GetString(),
                row.Cell(4).GetString(),
                row.Cell(5).GetString(),
            });
        }
        return rows;
    }

    // RFC-4180-compliant CSV split (handles quoted commas)
    private static string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
        foreach (var ch in line)
        {
            if (ch == '"') inQuotes = !inQuotes;
            else if (ch == ',' && !inQuotes) { fields.Add(current.ToString().Trim()); current.Clear(); }
            else current.Append(ch);
        }
        fields.Add(current.ToString().Trim());
        return fields.ToArray();
    }

    internal static ImportRowDto ValidateRow(string[] cols, int rowNum, List<string> categoriesLower)
    {
        // Expected columns: Title | Description | Amount | Date (YYYY-MM-DD) | Category
        if (cols.Length < 5)
            return Fail(rowNum, cols, 0,
                $"Row {rowNum}: Expected 5 columns — Title, Description, Amount, Date, Category.");

        var title = cols[0].Trim();
        var desc = cols[1].Trim();
        var amountStr = cols[2].Trim();
        var dateStr = cols[3].Trim();
        var category = cols[4].Trim();

        if (string.IsNullOrEmpty(title))
            return Fail(rowNum, cols, 0, $"Row {rowNum}: Title is required.");

        if (!decimal.TryParse(amountStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var amount) || amount <= 0)
            return Fail(rowNum, cols, 0,
                $"Row {rowNum}: Invalid amount '{amountStr}' — must be a positive number.");

        if (!DateTime.TryParse(dateStr, out _))
            return Fail(rowNum, cols, amount,
                $"Row {rowNum}: Invalid date '{dateStr}' — use YYYY-MM-DD.");

        if (!categoriesLower.Contains(category.ToLower()))
            return Fail(rowNum, cols, amount,
                $"Row {rowNum}: Category '{category}' not found.");

        return new ImportRowDto(rowNum, title, desc, amount, dateStr, category, true, null);
    }

    private static ImportRowDto Fail(int rowNum, string[] cols, decimal amount, string error)
        => new(rowNum,
               cols.ElementAtOrDefault(0)?.Trim() ?? "",
               cols.ElementAtOrDefault(1)?.Trim() ?? "",
               amount,
               cols.ElementAtOrDefault(3)?.Trim() ?? "",
               cols.ElementAtOrDefault(4)?.Trim() ?? "",
               false, error);
}

// ─── PREVIEW QUERY ────────────────────────────────────────────────────────────

public record PreviewImportCommand(IFormFile File) : IRequest<ImportPreviewDto>;

public class PreviewImportCommandHandler
    : IRequestHandler<PreviewImportCommand, ImportPreviewDto>
{
    private readonly IApplicationDbContext _context;

    public PreviewImportCommandHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<ImportPreviewDto> Handle(
        PreviewImportCommand request, CancellationToken ct)
    {
        var categoryNames = await _context.Categories.AsNoTracking()
            .Select(c => c.Name).ToListAsync(ct);
        var categoriesLower = categoryNames.Select(n => n.ToLower()).ToList();

        var rows = ExpenseImportParser.Parse(request.File);
        var parsed = rows.Select((r, i) =>
            ExpenseImportParser.ValidateRow(r, i + 2, categoriesLower)).ToList();

        return new ImportPreviewDto(
            parsed,
            parsed.Count(r => r.IsValid),
            parsed.Count(r => !r.IsValid),
            categoryNames);
    }
}

// ─── IMPORT COMMAND ───────────────────────────────────────────────────────────

public record ImportExpensesCommand(
    IFormFile File,
    bool SubmitAfterImport = false,
    bool SkipErrors = true) : IRequest<ImportResultDto>;

public class ImportExpensesCommandHandler
    : IRequestHandler<ImportExpensesCommand, ImportResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ImportExpensesCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task<ImportResultDto> Handle(
        ImportExpensesCommand request, CancellationToken ct)
    {
        var categoryLookup = await _context.Categories.AsNoTracking()
            .Select(c => new { c.Id, Name = c.Name.ToLower() })
            .ToDictionaryAsync(c => c.Name, c => c.Id, ct);

        var rows = ExpenseImportParser.Parse(request.File);
        var imported = 0;
        var skipped = 0;
        var errors = new List<string>();

        foreach (var (cols, idx) in rows.Select((r, i) => (r, i + 2)))
        {
            if (cols.Length < 5) { skipped++; continue; }

            var title = cols[0].Trim();
            var desc = cols[1].Trim();
            var amountStr = cols[2].Trim();
            var dateStr = cols[3].Trim();
            var catKey = cols[4].Trim().ToLower();

            // ── Validate ────────────────────────────────────────────────
            if (string.IsNullOrEmpty(title))
            {
                errors.Add($"Row {idx}: Title is required.");
                skipped++; continue;
            }

            if (!decimal.TryParse(amountStr,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var amount) || amount <= 0)
            {
                errors.Add($"Row {idx}: Invalid amount '{amountStr}'.");
                skipped++;
                if (!request.SkipErrors) break;
                continue;
            }

            if (!DateTime.TryParse(dateStr, out var date))
            {
                errors.Add($"Row {idx}: Invalid date '{dateStr}'.");
                skipped++;
                if (!request.SkipErrors) break;
                continue;
            }

            if (!categoryLookup.TryGetValue(catKey, out var categoryId))
            {
                errors.Add($"Row {idx}: Category '{cols[4].Trim()}' not found.");
                skipped++;
                if (!request.SkipErrors) break;
                continue;
            }

            // ── Create ──────────────────────────────────────────────────
            var expense = Expense.Create(
                title,
                string.IsNullOrEmpty(desc) ? null : desc,
                amount,
                DateTime.SpecifyKind(date, DateTimeKind.Utc),
                categoryId,
                _currentUser.UserId,
                _currentUser.TenantId);

            // Always Draft — SubmitAfterImport kept for future flexibility
            if (request.SubmitAfterImport)
                expense.Submit();

            _context.Expenses.Add(expense);
            imported++;
        }

        if (imported > 0)
            await _context.SaveChangesAsync(ct);

        return new ImportResultDto(imported, skipped, errors);
    }
}

// ─── TEMPLATE DOWNLOAD ────────────────────────────────────────────────────────

public record GetImportTemplateQuery : IRequest<byte[]>;

public class GetImportTemplateQueryHandler
    : IRequestHandler<GetImportTemplateQuery, byte[]>
{
    public Task<byte[]> Handle(GetImportTemplateQuery request, CancellationToken ct)
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Title,Description,Amount,Date,Category");
        csv.AppendLine("Office Supplies,Printer paper and pens,45.99,2026-03-01,Office");
        csv.AppendLine("Team Lunch,Quarterly team lunch,120.00,2026-03-05,Food & Dining");
        csv.AppendLine("Flight to NYC,Conference travel,380.00,2026-03-10,Travel");
        return Task.FromResult(System.Text.Encoding.UTF8.GetBytes(csv.ToString()));
    }
}