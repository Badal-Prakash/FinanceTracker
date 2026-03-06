using System;
using FinanceTracker.Application.Common.Exceptions;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Application.Dtos;
using FinanceTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Expenses.Queries.GetExpenseById;



public class GetExpenseByIdQueryHandler : IRequestHandler<GetExpenseByIdQuery, ExpenseDto>
{
    private readonly IApplicationDbContext _context;

    public GetExpenseByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<ExpenseDto> Handle(GetExpenseByIdQuery request, CancellationToken ct)
    {
        var e = await _context.Expenses
            .Include(x => x.Category)
            .Include(x => x.SubmittedBy)
            .Include(x => x.Approver)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Expense), request.Id);

        return new ExpenseDto(
            e.Id, e.Title, e.Description, e.Amount, e.ExpenseDate,
            e.Status.ToString(), e.ReceiptUrl, e.RejectionReason,
            e.SubmittedBy!.FullName,
            e.Approver?.FullName,
            e.Category!.Name, e.Category.Color,
            e.CreatedAt);
    }
}