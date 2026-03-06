using System;
using FinanceTracker.Application.Common.Exceptions;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Expenses.Commands.DeleteExpense;

public class DeleteExpenseCommandHandler : IRequestHandler<DeleteExpenseCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteExpenseCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteExpenseCommand request, CancellationToken ct)
    {
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseId, ct)
            ?? throw new NotFoundException(nameof(Expense), request.ExpenseId);

        if (expense.SubmittedById != _currentUser.UserId && _currentUser.Role == UserRole.Employee.ToString())
            throw new ForbiddenException("You can only delete your own draft expenses.");

        if (expense.Status != ExpenseStatus.Draft)
            throw new InvalidOperationException("Only draft expenses can be deleted.");

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync(ct);
    }
}
