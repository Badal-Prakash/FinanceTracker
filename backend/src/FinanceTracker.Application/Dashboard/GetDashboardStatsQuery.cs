using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Dashboard;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record DashboardStatsDto(
    decimal TotalExpensesThisMonth,
    int PendingApprovalsCount,
    int ApprovedThisMonth,
    int TotalCategories,
    decimal TotalApprovedAmountThisMonth,
    List<CategoryExpenseDto> TopCategories,
    List<MonthlyTrendDto> MonthlyTrend);

public record CategoryExpenseDto(string CategoryName, string Color, decimal TotalAmount, int Count);

public record MonthlyTrendDto(int Year, int Month, string MonthName, decimal TotalAmount);

// ─── QUERY ────────────────────────────────────────────────────────────────────

public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetDashboardStatsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);

        // Base query — employees see only their own
        var baseQuery = _context.Expenses.AsNoTracking();
        if (_currentUser.Role == "Employee")
            baseQuery = baseQuery.Where(e => e.SubmittedById == _currentUser.UserId);

        // All expenses this month
        var thisMonthExpenses = baseQuery
            .Where(e => e.ExpenseDate >= startOfMonth && e.ExpenseDate < endOfMonth);

        var totalThisMonth = await thisMonthExpenses
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0;

        var pendingCount = await baseQuery
            .CountAsync(e => e.Status == ExpenseStatus.Submitted, ct);

        var approvedThisMonth = await thisMonthExpenses
            .CountAsync(e => e.Status == ExpenseStatus.Approved, ct);

        var approvedAmountThisMonth = await thisMonthExpenses
            .Where(e => e.Status == ExpenseStatus.Approved)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0;

        var totalCategories = await _context.Categories
            .CountAsync(c => c.TenantId == _currentUser.TenantId && c.IsActive, ct);

        // Top 5 categories by spend (last 3 months)
        var threeMonthsAgo = startOfMonth.AddMonths(-3);
        var topCategories = await baseQuery
            .Where(e => e.ExpenseDate >= threeMonthsAgo)
            .Include(e => e.Category)
            .GroupBy(e => new { e.CategoryId, e.Category!.Name, e.Category.Color })
            .Select(g => new CategoryExpenseDto(
                g.Key.Name,
                g.Key.Color,
                g.Sum(e => e.Amount),
                g.Count()))
            .OrderByDescending(c => c.TotalAmount)
            .Take(5)
            .ToListAsync(ct);

        // Monthly trend (last 6 months)
        var sixMonthsAgo = startOfMonth.AddMonths(-5);
        var monthlyData = await baseQuery
            .Where(e => e.ExpenseDate >= sixMonthsAgo)
            .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(e => e.Amount) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(ct);

        var monthlyTrend = monthlyData.Select(m => new MonthlyTrendDto(
            m.Year, m.Month,
            new DateTime(m.Year, m.Month, 1).ToString("MMM"),
            m.Total)).ToList();

        return new DashboardStatsDto(
            totalThisMonth,
            pendingCount,
            approvedThisMonth,
            totalCategories,
            approvedAmountThisMonth,
            topCategories,
            monthlyTrend);
    }
}