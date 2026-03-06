using System;
using FluentValidation;

namespace FinanceTracker.Application.Expenses.Commands.RejectExpense;

public class RejectExpenseCommandValidator : AbstractValidator<RejectExpenseCommand>
{
    public RejectExpenseCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
