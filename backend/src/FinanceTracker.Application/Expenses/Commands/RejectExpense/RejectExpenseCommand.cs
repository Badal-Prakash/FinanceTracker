using System;
using MediatR;

namespace FinanceTracker.Application.Expenses.Commands.RejectExpense;

public class RejectExpenseCommand : IRequest
{
    public RejectExpenseCommand(Guid expenseId, string reason)
    {
        ExpenseId = expenseId;
        Reason = reason;
    }

    public Guid ExpenseId { get; set; }
    public string Reason { get; set; } = string.Empty;
}