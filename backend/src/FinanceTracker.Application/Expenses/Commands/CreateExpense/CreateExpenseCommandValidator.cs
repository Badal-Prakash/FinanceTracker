using System;
using FluentValidation;

namespace FinanceTracker.Application.Expenses.Commands.CreateExpense;

public class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThan(0).LessThan(1_000_000);
        RuleFor(x => x.ExpenseDate).LessThanOrEqualTo(DateTime.UtcNow.AddDays(1));
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}
