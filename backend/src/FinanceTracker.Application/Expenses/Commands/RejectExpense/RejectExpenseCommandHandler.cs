using System;
using FinanceTracker.Application.Common.Exceptions;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Expenses.Commands.RejectExpense;

public class RejectExpenseCommandHandler : IRequestHandler<RejectExpenseCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public RejectExpenseCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(RejectExpenseCommand request, CancellationToken ct)
    {
        if (_currentUser.Role == UserRole.Employee.ToString())
            throw new ForbiddenException("Only managers and admins can reject expenses.");

        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseId, ct)
            ?? throw new NotFoundException(nameof(Expense), request.ExpenseId);

        expense.Reject(_currentUser.UserId, request.Reason);
        await _context.SaveChangesAsync(ct);
    }
}
