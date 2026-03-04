using FinanceTracker.Application.Common.Exceptions;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Budgets;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record BudgetDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string CategoryColor,
    string CategoryIcon,
    decimal BudgetedAmount,
    decimal SpentAmount,
    decimal RemainingAmount,
    decimal UtilisationPercent,
    string AlertLevel,      // "ok" | "warning" | "critical" | "exceeded"
    int Month,
    int Year);

public record BudgetSummaryDto(
    int Month,
    int Year,
    string MonthLabel,
    decimal TotalBudgeted,
    decimal TotalSpent,
    decimal TotalRemaining,
    decimal OverallUtilisationPercent,
    List<BudgetDto> Categories,
    List<BudgetAlertDto> Alerts);

public record BudgetAlertDto(
    string CategoryName,
    string CategoryColor,
    decimal BudgetedAmount,
    decimal SpentAmount,
    decimal UtilisationPercent,
    string Level);

public record BudgetTrendDto(
    int Month,
    int Year,
    string MonthLabel,
    decimal TotalBudgeted,
    decimal TotalSpent,
    decimal UtilisationPercent);

// ─── QUERIES ──────────────────────────────────────────────────────────────────

public record GetBudgetSummaryQuery(int? Month = null, int? Year = null)
    : IRequest<BudgetSummaryDto>;

public class GetBudgetSummaryQueryHandler
    : IRequestHandler<GetBudgetSummaryQuery, BudgetSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetBudgetSummaryQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<BudgetSummaryDto> Handle(
        GetBudgetSummaryQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var month = request.Month ?? now.Month;
        var year = request.Year ?? now.Year;

        var budgets = await _context.Budgets
            .Include(b => b.Category)
            .Where(b => b.Month == month && b.Year == year)
            .AsNoTracking()
            .ToListAsync(ct);

        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var spentByCat = await _context.Expenses
            .Where(e => e.Status == ExpenseStatus.Approved
                     && e.ExpenseDate >= start
                     && e.ExpenseDate < end)
            .GroupBy(e => e.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(e => e.Amount) })
            .AsNoTracking()
            .ToDictionaryAsync(e => e.CategoryId, e => e.Total, ct);

        var categoryDtos = budgets
            .Where(b => b.Category != null)
            .Select(b =>
            {
                var spent = spentByCat.GetValueOrDefault(b.CategoryId, 0m);
                var pct = b.Amount > 0
                    ? Math.Round(spent / b.Amount * 100, 1) : 0m;
                return new BudgetDto(
                    b.Id, b.CategoryId,
                    b.Category!.Name, b.Category.Color, b.Category.Icon,
                    b.Amount, spent, b.Amount - spent, pct,
                    GetAlertLevel(pct), month, year);
            })
            .OrderByDescending(b => b.UtilisationPercent)
            .ToList();

        var alerts = categoryDtos
            .Where(b => b.AlertLevel != "ok")
            .Select(b => new BudgetAlertDto(
                b.CategoryName, b.CategoryColor,
                b.BudgetedAmount, b.SpentAmount,
                b.UtilisationPercent, b.AlertLevel))
            .ToList();

        var totalBudgeted = categoryDtos.Sum(b => b.BudgetedAmount);
        var totalSpent = categoryDtos.Sum(b => b.SpentAmount);
        var overallPct = totalBudgeted > 0
            ? Math.Round(totalSpent / totalBudgeted * 100, 1) : 0m;

        return new BudgetSummaryDto(
            month, year,
            new DateTime(year, month, 1).ToString("MMMM yyyy"),
            totalBudgeted, totalSpent,
            totalBudgeted - totalSpent,
            overallPct,
            categoryDtos,
            alerts);
    }

    private static string GetAlertLevel(decimal pct) => pct switch
    {
        > 100 => "exceeded",
        >= 90 => "critical",
        >= 80 => "warning",
        _ => "ok"
    };
}

public record GetBudgetTrendQuery(int Months = 6) : IRequest<List<BudgetTrendDto>>;

public class GetBudgetTrendQueryHandler
    : IRequestHandler<GetBudgetTrendQuery, List<BudgetTrendDto>>
{
    private readonly IApplicationDbContext _context;

    public GetBudgetTrendQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<List<BudgetTrendDto>> Handle(
        GetBudgetTrendQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1)
            .AddMonths(-(request.Months - 1));

        var budgetsByMonth = await _context.Budgets
            .Where(b => new DateTime(b.Year, b.Month, 1) >= start)
            .GroupBy(b => new { b.Year, b.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(b => b.Amount) })
            .AsNoTracking()
            .ToListAsync(ct);

        var spentByMonth = await _context.Expenses
            .Where(e => e.Status == ExpenseStatus.Approved && e.ExpenseDate >= start)
            .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(e => e.Amount) })
            .AsNoTracking()
            .ToListAsync(ct);

        var budgetLookup = budgetsByMonth
            .ToDictionary(b => (b.Year, b.Month), b => b.Total);
        var spentLookup = spentByMonth
            .ToDictionary(e => (e.Year, e.Month), e => e.Total);

        return Enumerable.Range(0, request.Months)
            .Select(i =>
            {
                var d = start.AddMonths(i);
                var key = (d.Year, d.Month);
                var budgeted = budgetLookup.GetValueOrDefault(key, 0m);
                var spent = spentLookup.GetValueOrDefault(key, 0m);
                var pct = budgeted > 0
                    ? Math.Round(spent / budgeted * 100, 1) : 0m;
                return new BudgetTrendDto(
                    d.Month, d.Year,
                    d.ToString("MMM yyyy"),
                    budgeted, spent, pct);
            })
            .ToList();
    }
}

// ─── COMMANDS ─────────────────────────────────────────────────────────────────

public record SetBudgetCommand(
    Guid CategoryId,
    decimal Amount,
    int Month,
    int Year) : IRequest<Guid>;

public class SetBudgetCommandValidator : AbstractValidator<SetBudgetCommand>
{
    public SetBudgetCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).LessThan(10_000_000);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Year).InclusiveBetween(2020, 2100);
    }
}

public class SetBudgetCommandHandler : IRequestHandler<SetBudgetCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SetBudgetCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task<Guid> Handle(SetBudgetCommand request, CancellationToken ct)
    {
        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == request.CategoryId, ct);
        if (!categoryExists)
            throw new NotFoundException(nameof(Category), request.CategoryId);

        var existing = await _context.Budgets
            .FirstOrDefaultAsync(b =>
                b.CategoryId == request.CategoryId &&
                b.Month == request.Month &&
                b.Year == request.Year, ct);

        if (existing is not null)
        {
            existing.UpdateAmount(request.Amount);
            await _context.SaveChangesAsync(ct);
            return existing.Id;
        }

        var budget = Budget.Create(
            request.CategoryId, request.Amount,
            request.Month, request.Year,
            _currentUser.TenantId);

        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync(ct);
        return budget.Id;
    }
}

public record CopyBudgetsCommand(
    int FromMonth, int FromYear,
    int ToMonth, int ToYear) : IRequest<int>;

public class CopyBudgetsCommandValidator : AbstractValidator<CopyBudgetsCommand>
{
    public CopyBudgetsCommandValidator()
    {
        RuleFor(x => x.FromMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.FromYear).InclusiveBetween(2020, 2100);
        RuleFor(x => x.ToMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.ToYear).InclusiveBetween(2020, 2100);
        RuleFor(x => x).Must(x =>
            !(x.FromMonth == x.ToMonth && x.FromYear == x.ToYear))
            .WithMessage("Source and destination month cannot be the same.");
    }
}

public class CopyBudgetsCommandHandler : IRequestHandler<CopyBudgetsCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CopyBudgetsCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task<int> Handle(CopyBudgetsCommand request, CancellationToken ct)
    {
        var source = await _context.Budgets
            .Where(b => b.Month == request.FromMonth && b.Year == request.FromYear)
            .AsNoTracking().ToListAsync(ct);

        if (!source.Any())
            throw new InvalidOperationException(
                "No budgets found for the source month.");

        var count = 0;
        foreach (var src in source)
        {
            var existing = await _context.Budgets.FirstOrDefaultAsync(b =>
                b.CategoryId == src.CategoryId &&
                b.Month == request.ToMonth &&
                b.Year == request.ToYear, ct);

            if (existing is not null) existing.UpdateAmount(src.Amount);
            else _context.Budgets.Add(Budget.Create(
                src.CategoryId, src.Amount,
                request.ToMonth, request.ToYear,
                _currentUser.TenantId));
            count++;
        }

        await _context.SaveChangesAsync(ct);
        return count;
    }
}

public record DeleteBudgetCommand(Guid Id) : IRequest;

public class DeleteBudgetCommandHandler : IRequestHandler<DeleteBudgetCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteBudgetCommandHandler(IApplicationDbContext context)
        => _context = context;

    public async Task Handle(DeleteBudgetCommand request, CancellationToken ct)
    {
        var budget = await _context.Budgets
            .FirstOrDefaultAsync(b => b.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Budget), request.Id);

        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync(ct);
    }
}