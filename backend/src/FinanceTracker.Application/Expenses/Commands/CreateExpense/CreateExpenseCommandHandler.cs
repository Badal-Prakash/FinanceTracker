using System;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Entities;
using MediatR;

namespace FinanceTracker.Application.Expenses.Commands.CreateExpense;

public class CreateExpenseCommandHandler : IRequestHandler<CreateExpenseCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateExpenseCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateExpenseCommand request, CancellationToken ct)
    {
        var expense = Expense.Create(
            request.Title, request.Description, request.Amount,
            request.ExpenseDate, request.CategoryId,
            _currentUser.UserId, _currentUser.TenantId, request.ReceiptUrl);

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync(ct);
        return expense.Id;
    }
}
