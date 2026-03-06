using System;
using FinanceTracker.Application.Common.Exceptions;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Expenses.Commands.ApproveExpense;

public class ApproveExpenseCommandHandler : IRequestHandler<ApproveExpenseCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ApproveExpenseCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(ApproveExpenseCommand request, CancellationToken ct)
    {
        if (_currentUser.Role == UserRole.Employee.ToString())
            throw new ForbiddenException("Only managers and admins can approve expenses.");

        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseId, ct)
            ?? throw new NotFoundException(nameof(Expense), request.ExpenseId);

        expense.Approve(_currentUser.UserId);
        await _context.SaveChangesAsync(ct);
    }
}
