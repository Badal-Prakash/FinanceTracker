using System;
using FinanceTracker.Application.Dtos;
using MediatR;

namespace FinanceTracker.Application.Expenses.Queries.GetExpenseById;

public class GetExpenseByIdQuery : IRequest<ExpenseDto>
{
    public Guid Id { get; set; }
    public GetExpenseByIdQuery(Guid id) => Id = id;
}