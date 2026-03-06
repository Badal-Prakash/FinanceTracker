using System;
using MediatR;

namespace FinanceTracker.Application.Expenses.Commands.SubmitExpense;

public class SubmitExpenseCommand : IRequest
{
    public Guid ExpenseId { get; set; }

    public SubmitExpenseCommand(Guid ExpenseId)
    {
        this.ExpenseId = ExpenseId;
    }
}