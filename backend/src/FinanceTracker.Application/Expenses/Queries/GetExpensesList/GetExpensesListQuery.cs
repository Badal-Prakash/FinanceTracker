using System;
using MediatR;
using FinanceTracker.Application.Dtos;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Invoices;

namespace FinanceTracker.Application.Expenses.Queries.GetExpensesList;

public class GetExpensesListQuery : IRequest<PaginatedList<ExpenseListDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Status { get; set; }
    public Guid? CategoryId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? SubmittedById { get; set; }
}