using System;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Dtos;
using FinanceTracker.Application.Invoices;
using FinanceTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Expenses.Queries.GetExpensesList;

public class GetExpensesListQueryHandler : IRequestHandler<GetExpensesListQuery, PaginatedList<ExpenseListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetExpensesListQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PaginatedList<ExpenseListDto>> Handle(GetExpensesListQuery request, CancellationToken ct)
    {
        var query = _context.Expenses
            .Include(e => e.Category)
            .Include(e => e.SubmittedBy)
            .AsNoTracking()
            .AsQueryable();

        // Employees see only their own expenses
        if (_currentUser.Role == UserRole.Employee.ToString())
            query = query.Where(e => e.SubmittedById == _currentUser.UserId);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<ExpenseStatus>(request.Status, out var status))
            query = query.Where(e => e.Status == status);

        if (request.CategoryId.HasValue)
            query = query.Where(e => e.CategoryId == request.CategoryId.Value);

        if (request.FromDate.HasValue)
            query = query.Where(e => e.ExpenseDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(e => e.ExpenseDate <= request.ToDate.Value);

        if (request.SubmittedById.HasValue)
            query = query.Where(e => e.SubmittedById == request.SubmittedById.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new ExpenseListDto(
                e.Id, e.Title, e.Amount, e.ExpenseDate,
                e.Status.ToString(),
                e.Category!.Name, e.Category.Color,
                e.SubmittedBy!.FirstName + " " + e.SubmittedBy.LastName,
                e.CreatedAt))
            .ToListAsync(ct);

        return new PaginatedList<ExpenseListDto>(items, totalCount, request.Page, request.PageSize);
    }
}
