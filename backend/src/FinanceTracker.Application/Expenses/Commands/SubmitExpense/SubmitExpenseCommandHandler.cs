using System;
using FinanceTracker.Application.Common.Exceptions;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Expenses.Commands.SubmitExpense;

public class SubmitExpenseCommandHandler : IRequestHandler<SubmitExpenseCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SubmitExpenseCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(SubmitExpenseCommand request, CancellationToken ct)
    {
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseId, ct)
            ?? throw new NotFoundException(nameof(Expense), request.ExpenseId);

        if (expense.SubmittedById != _currentUser.UserId)
            throw new ForbiddenException("You can only submit your own expenses.");

        expense.Submit();
        await _context.SaveChangesAsync(ct);
    }
}

