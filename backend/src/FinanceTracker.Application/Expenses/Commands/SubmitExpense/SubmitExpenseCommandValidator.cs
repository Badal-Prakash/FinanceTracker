using FinanceTracker.Application.Expenses.Commands.SubmitExpense;
using FluentValidation;

namespace FinanceTracker.Application.Expenses.Commands;

public class SubmitExpenseCommandValidator : AbstractValidator<SubmitExpenseCommand>
{
    public SubmitExpenseCommandValidator()
    {
        RuleFor(v => v.ExpenseId)
            .NotEmpty()
            .WithMessage("A valid Expense ID must be provided.");
    }
}