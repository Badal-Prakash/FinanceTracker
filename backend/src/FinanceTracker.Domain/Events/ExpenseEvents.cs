using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Domain.Events;

public class ExpenseSubmittedEvent : BaseEvent
{
    public Expense Expense { get; }
    public ExpenseSubmittedEvent(Expense expense) => Expense = expense;
}

public class ExpenseApprovedEvent : BaseEvent
{
    public Expense Expense { get; }
    public ExpenseApprovedEvent(Expense expense) => Expense = expense;
}

public class ExpenseRejectedEvent : BaseEvent
{
    public Expense Expense { get; }
    public ExpenseRejectedEvent(Expense expense) => Expense = expense;
}