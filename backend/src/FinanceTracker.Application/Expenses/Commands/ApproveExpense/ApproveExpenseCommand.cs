using System;
using MediatR;

namespace FinanceTracker.Application.Expenses.Commands.ApproveExpense;

public class ApproveExpenseCommand : IRequest
{
    public ApproveExpenseCommand(Guid id)
    {
        ExpenseId = id;
    }

    public Guid ExpenseId { get; set; }
}
