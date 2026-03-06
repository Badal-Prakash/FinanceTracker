using System;
using MediatR;

namespace FinanceTracker.Application.Expenses.Commands.DeleteExpense;

public class DeleteExpenseCommand : IRequest
{
    public DeleteExpenseCommand(Guid id)
    {
        ExpenseId = id;
    }
    public Guid ExpenseId { get; set; }
}