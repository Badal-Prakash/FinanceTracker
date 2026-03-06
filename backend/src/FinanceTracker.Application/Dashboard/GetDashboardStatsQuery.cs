// using FinanceTracker.Application.Common.Interfaces;
// using FinanceTracker.Domain.Enums;
// using MediatR;
// using Microsoft.EntityFrameworkCore;

// namespace FinanceTracker.Application.Dashboard;
// public record DashboardStatsDto(
//     decimal TotalExpensesThisMonth,
//     int PendingApprovalsCount,
//     int ApprovedThisMonth,
//     int TotalCategories,
//     decimal TotalApprovedAmountThisMonth,
//     List<CategoryExpenseDto> TopCategories,
//     List<MonthlyTrendDto> MonthlyTrend);

// public record CategoryExpenseDto(string CategoryName, string Color, decimal TotalAmount, int Count);

// public record MonthlyTrendDto(int Year, int Month, string MonthName, decimal TotalAmount);

// // ─── QUERY ────────────────────────────────────────────────────────────────────

// public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

// public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
// {
//     private readonly IApplicationDbContext _context;
//     private readonly ICurrentUserService _currentUser;

//     public GetDashboardStatsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
//     {
//         _context = context;
//         _currentUser = currentUser;
//     }

//     public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken ct)
//     {
//         var now = DateTime.UtcNow;
//         var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
//         var endOfMonth = startOfMonth.AddMonths(1);

//         // Base query — employees see only their own
//         var baseQuery = _context.Expenses.AsNoTracking();
//         if (_currentUser.Role == "Employee")
//             baseQuery = baseQuery.Where(e => e.SubmittedById == _currentUser.UserId);

//         // All expenses this month
//         var thisMonthExpenses = baseQuery
//             .Where(e => e.ExpenseDate >= startOfMonth && e.ExpenseDate < endOfMonth);

//         var totalThisMonth = await thisMonthExpenses
//             .SumAsync(e => (decimal?)e.Amount, ct) ?? 0;

//         var pendingCount = await baseQuery
//             .CountAsync(e => e.Status == ExpenseStatus.Submitted, ct);

//         var approvedThisMonth = await thisMonthExpenses
//             .CountAsync(e => e.Status == ExpenseStatus.Approved, ct);

//         var approvedAmountThisMonth = await thisMonthExpenses
//             .Where(e => e.Status == ExpenseStatus.Approved)
//             .SumAsync(e => (decimal?)e.Amount, ct) ?? 0;

//         var totalCategories = await _context.Categories
//             .CountAsync(c => c.TenantId == _currentUser.TenantId && c.IsActive, ct);

//         // Top 5 categories by spend (last 3 months)
//         var threeMonthsAgo = startOfMonth.AddMonths(-3);
//         var topCategories = await baseQuery
//             .Where(e => e.ExpenseDate >= threeMonthsAgo)
//             .Include(e => e.Category)
//             .GroupBy(e => new { e.CategoryId, e.Category!.Name, e.Category.Color })
//             .Select(g => new CategoryExpenseDto(
//                 g.Key.Name,
//                 g.Key.Color,
//                 g.Sum(e => e.Amount),
//                 g.Count()))
//             .OrderByDescending(c => c.TotalAmount)
//             .Take(5)
//             .ToListAsync(ct);

//         // Monthly trend (last 6 months)
//         var sixMonthsAgo = startOfMonth.AddMonths(-5);
//         var monthlyData = await baseQuery
//             .Where(e => e.ExpenseDate >= sixMonthsAgo)
//             .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
//             .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(e => e.Amount) })
//             .OrderBy(x => x.Year).ThenBy(x => x.Month)
//             .ToListAsync(ct);

//         var monthlyTrend = monthlyData.Select(m => new MonthlyTrendDto(
//             m.Year, m.Month,
//             new DateTime(m.Year, m.Month, 1).ToString("MMM"),
//             m.Total)).ToList();

//         return new DashboardStatsDto(
//             totalThisMonth,
//             pendingCount,
//             approvedThisMonth,
//             totalCategories,
//             approvedAmountThisMonth,
//             topCategories,
//             monthlyTrend);
//     }
// }
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Dashboard;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record DashboardStatsDto(
    // KPIs
    decimal TotalExpensesThisMonth,
    decimal TotalExpensesLastMonth,
    decimal MonthOverMonthChange,        // % change vs last month
    int PendingApprovalsCount,
    int ApprovedThisMonth,
    int RejectedThisMonth,
    decimal ApprovalRate,                // % of submitted that got approved
    decimal TotalApprovedAmountThisMonth,
    int TotalCategories,
    // Budget
    decimal TotalBudgetedThisMonth,
    decimal BudgetUtilisationPercent,
    // Charts
    List<CategoryExpenseDto> TopCategories,
    List<MonthlyTrendDto> MonthlyTrend,
    List<StatusBreakdownDto> StatusBreakdown,
    // Recent
    List<RecentExpenseDto> RecentExpenses);

public record CategoryExpenseDto(
    string CategoryName,
    string Color,
    decimal TotalAmount,
    int Count,
    decimal Percentage);

public record MonthlyTrendDto(
    int Year,
    int Month,
    string MonthName,
    decimal TotalAmount,
    decimal ApprovedAmount);

public record StatusBreakdownDto(
    string Status,
    int Count,
    decimal TotalAmount);

public record RecentExpenseDto(
    Guid Id,
    string Title,
    decimal Amount,
    string Status,
    string CategoryName,
    string CategoryColor,
    DateTime ExpenseDate,
    string SubmittedBy);

// ─── QUERY ────────────────────────────────────────────────────────────────────

public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

public class GetDashboardStatsQueryHandler
    : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetDashboardStatsQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task<DashboardStatsDto> Handle(
        GetDashboardStatsQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var startThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startLastMonth = startThisMonth.AddMonths(-1);
        var startSixMonths = startThisMonth.AddMonths(-5);

        // Role-based scope
        var base_ = _context.Expenses.AsNoTracking().AsQueryable();
        if (_currentUser.Role == "Employee")
            base_ = base_.Where(e => e.SubmittedById == _currentUser.UserId);

        // ── This month ────────────────────────────────────────────────────────
        var thisMonth = base_.Where(e =>
            e.ExpenseDate >= startThisMonth && e.ExpenseDate < startThisMonth.AddMonths(1));

        var totalThisMonth = await thisMonth
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0;

        var approvedAmtThisMonth = await thisMonth
            .Where(e => e.Status == ExpenseStatus.Approved)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0;

        var approvedCntThisMonth = await thisMonth
            .CountAsync(e => e.Status == ExpenseStatus.Approved, ct);

        var rejectedCntThisMonth = await thisMonth
            .CountAsync(e => e.Status == ExpenseStatus.Rejected, ct);

        var pendingCount = await base_
            .CountAsync(e => e.Status == ExpenseStatus.Submitted, ct);

        // ── Last month ────────────────────────────────────────────────────────
        var totalLastMonth = await base_
            .Where(e => e.ExpenseDate >= startLastMonth && e.ExpenseDate < startThisMonth)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0;

        var momChange = totalLastMonth > 0
            ? Math.Round((totalThisMonth - totalLastMonth) / totalLastMonth * 100, 1)
            : 0;

        // ── Approval rate (last 3 months) ─────────────────────────────────────
        var threeMonthsAgo = startThisMonth.AddMonths(-3);
        var decidedCount = await base_
            .Where(e => e.ExpenseDate >= threeMonthsAgo &&
                        (e.Status == ExpenseStatus.Approved ||
                         e.Status == ExpenseStatus.Rejected))
            .CountAsync(ct);
        var approvedCount3m = await base_
            .Where(e => e.ExpenseDate >= threeMonthsAgo &&
                        e.Status == ExpenseStatus.Approved)
            .CountAsync(ct);
        var approvalRate = decidedCount > 0
            ? Math.Round((decimal)approvedCount3m / decidedCount * 100, 1) : 0;

        // ── Categories ────────────────────────────────────────────────────────
        var totalCategories = await _context.Categories
            .CountAsync(c => c.IsActive, ct);

        // ── Top categories (last 3 months) ───────────────────────────────────
        // Load category lookup first (separate query, no translation issues)
        var categoryLookup = await _context.Categories
            .AsNoTracking()
            .Select(c => new { c.Id, c.Name, c.Color })
            .ToDictionaryAsync(c => c.Id, ct);

        // Pull expense rows to memory — no Include, no navigation properties
        var catRaw = await base_
            .Where(e => e.ExpenseDate >= threeMonthsAgo)
            .Select(e => new { e.CategoryId, e.Amount })
            .ToListAsync(ct);

        var catData = catRaw
            .GroupBy(e => e.CategoryId)
            .Select(g =>
            {
                categoryLookup.TryGetValue(g.Key, out var cat);
                return new
                {
                    Name = cat?.Name ?? "Uncategorised",
                    Color = cat?.Color ?? "#6366f1",
                    Total = g.Sum(e => e.Amount),
                    Count = g.Count()
                };
            })
            .OrderByDescending(c => c.Total)
            .Take(6)
            .ToList();

        var catTotal = catData.Sum(c => c.Total);
        var topCategories = catData.Select(c => new CategoryExpenseDto(
            c.Name, c.Color, c.Total, c.Count,
            catTotal > 0 ? Math.Round(c.Total / catTotal * 100, 1) : 0))
            .ToList();

        // ── Monthly trend (last 6 months) ─────────────────────────────────────
        // Pull to memory first to avoid EF translation issues with .Year/.Month
        var monthlyRawData = await base_
            .Where(e => e.ExpenseDate >= startSixMonths)
            .Select(e => new
            {
                e.ExpenseDate,
                e.Amount,
                e.Status
            })
            .ToListAsync(ct);

        var monthlyLookup = monthlyRawData
            .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
            .ToDictionary(
                g => (g.Key.Year, g.Key.Month),
                g => new
                {
                    Total = g.Sum(e => e.Amount),
                    Approved = g.Where(e => e.Status == ExpenseStatus.Approved)
                                .Sum(e => e.Amount)
                });

        var monthlyTrend = Enumerable.Range(0, 6).Select(i =>
        {
            var d = startSixMonths.AddMonths(i);
            var key = (d.Year, d.Month);
            monthlyLookup.TryGetValue(key, out var row);
            return new MonthlyTrendDto(
                d.Year, d.Month,
                d.ToString("MMM"),
                row?.Total ?? 0,
                row?.Approved ?? 0);
        }).ToList();

        // ── Status breakdown (this month) ─────────────────────────────────────
        var statusRaw = await thisMonth
            .Select(e => new { Status = e.Status, Amount = e.Amount })
            .ToListAsync(ct);

        var statusBreakdown = statusRaw
            .GroupBy(e => e.Status)
            .Select(g => new StatusBreakdownDto(
                g.Key.ToString(),
                g.Count(),
                g.Sum(e => e.Amount)))
            .ToList();

        // ── Budget utilisation (this month) ───────────────────────────────────
        var totalBudgeted = await _context.Budgets
            .Where(b => b.Month == now.Month && b.Year == now.Year)
            .SumAsync(b => (decimal?)b.Amount, ct) ?? 0;

        var budgetUtil = totalBudgeted > 0
            ? Math.Round(approvedAmtThisMonth / totalBudgeted * 100, 1) : 0;

        // ── Recent 5 expenses ─────────────────────────────────────────────────
        // Load user lookup separately to avoid Include translation issues
        var userLookup = await _context.Users
            .AsNoTracking()
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToDictionaryAsync(u => u.Id, ct);

        var recentRaw = await base_
            .OrderByDescending(e => e.CreatedAt)
            .Take(5)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Amount,
                e.Status,
                e.CategoryId,
                e.SubmittedById,
                e.ExpenseDate
            })
            .ToListAsync(ct);

        var recent = recentRaw.Select(e =>
        {
            categoryLookup.TryGetValue(e.CategoryId, out var cat);
            userLookup.TryGetValue(e.SubmittedById, out var user);
            var fullName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown";
            return new RecentExpenseDto(
                e.Id, e.Title, e.Amount,
                e.Status.ToString(),
                cat?.Name ?? "Uncategorised",
                cat?.Color ?? "#6366f1",
                e.ExpenseDate,
                fullName);
        }).ToList();

        return new DashboardStatsDto(
            totalThisMonth, totalLastMonth, momChange,
            pendingCount,
            approvedCntThisMonth, rejectedCntThisMonth, approvalRate,
            approvedAmtThisMonth,
            totalCategories,
            totalBudgeted, budgetUtil,
            topCategories, monthlyTrend, statusBreakdown, recent);
    }
}