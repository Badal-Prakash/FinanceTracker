using System;
using MediatR;

namespace FinanceTracker.Application.Expenses.Commands.CreateExpense;

public class CreateExpenseCommand : IRequest<Guid>
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public Guid CategoryId { get; set; }
    public string? ReceiptUrl { get; set; }
}
