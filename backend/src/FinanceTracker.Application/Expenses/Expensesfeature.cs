using FinanceTracker.Application.Common.Exceptions;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Expenses;

// ─── DTOs ────────────────────────────────────────────────────────────────────
public record ExpenseDto(
    Guid Id,
    string Title,
    string? Description,
    decimal Amount,
    DateTime ExpenseDate,
    string Status,
    string? ReceiptUrl,
    string? RejectionReason,
    string SubmittedByName,
    string? ApproverName,
    string CategoryName,
    string CategoryColor,
    DateTime CreatedAt);

public record ExpenseListDto(
    Guid Id,
    string Title,
    decimal Amount,
    DateTime ExpenseDate,
    string Status,
    string CategoryName,
    string CategoryColor,
    string SubmittedByName,
    DateTime CreatedAt);

// ─── COMMANDS ─────────────────────────────────────────────────────────────────

// Create Expense
public record CreateExpenseCommand(
    string Title,
    string? Description,
    decimal Amount,
    DateTime ExpenseDate,
    Guid CategoryId,
    string? ReceiptUrl = null) : IRequest<Guid>;

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

public class CreateExpenseCommandHandler : IRequestHandler<CreateExpenseCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateExpenseCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateExpenseCommand request, CancellationToken ct)
    {
        var expense = Expense.Create(
            request.Title, request.Description, request.Amount,
            request.ExpenseDate, request.CategoryId,
            _currentUser.UserId, _currentUser.TenantId, request.ReceiptUrl);

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync(ct);
        return expense.Id;
    }
}

// Submit Expense
public record SubmitExpenseCommand(Guid ExpenseId) : IRequest;

public class SubmitExpenseCommandHandler : IRequestHandler<SubmitExpenseCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SubmitExpenseCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(SubmitExpenseCommand request, CancellationToken ct)
    {
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseId, ct)
            ?? throw new NotFoundException(nameof(Expense), request.ExpenseId);

        if (expense.SubmittedById != _currentUser.UserId)
            throw new ForbiddenException("You can only submit your own expenses.");

        expense.Submit();
        await _context.SaveChangesAsync(ct);
    }
}

// Approve Expense
public record ApproveExpenseCommand(Guid ExpenseId) : IRequest;

public class ApproveExpenseCommandHandler : IRequestHandler<ApproveExpenseCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ApproveExpenseCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(ApproveExpenseCommand request, CancellationToken ct)
    {
        if (_currentUser.Role == UserRole.Employee.ToString())
            throw new ForbiddenException("Only managers and admins can approve expenses.");

        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseId, ct)
            ?? throw new NotFoundException(nameof(Expense), request.ExpenseId);

        expense.Approve(_currentUser.UserId);
        await _context.SaveChangesAsync(ct);
    }
}

// Reject Expense
public record RejectExpenseCommand(Guid ExpenseId, string Reason) : IRequest;

public class RejectExpenseCommandValidator : AbstractValidator<RejectExpenseCommand>
{
    public RejectExpenseCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class RejectExpenseCommandHandler : IRequestHandler<RejectExpenseCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public RejectExpenseCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(RejectExpenseCommand request, CancellationToken ct)
    {
        if (_currentUser.Role == UserRole.Employee.ToString())
            throw new ForbiddenException("Only managers and admins can reject expenses.");

        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseId, ct)
            ?? throw new NotFoundException(nameof(Expense), request.ExpenseId);

        expense.Reject(_currentUser.UserId, request.Reason);
        await _context.SaveChangesAsync(ct);
    }
}

// Delete Expense
public record DeleteExpenseCommand(Guid ExpenseId) : IRequest;

public class DeleteExpenseCommandHandler : IRequestHandler<DeleteExpenseCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteExpenseCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteExpenseCommand request, CancellationToken ct)
    {
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseId, ct)
            ?? throw new NotFoundException(nameof(Expense), request.ExpenseId);

        if (expense.SubmittedById != _currentUser.UserId && _currentUser.Role == UserRole.Employee.ToString())
            throw new ForbiddenException("You can only delete your own draft expenses.");

        if (expense.Status != ExpenseStatus.Draft)
            throw new InvalidOperationException("Only draft expenses can be deleted.");

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync(ct);
    }
}

// ─── QUERIES ─────────────────────────────────────────────────────────────────

// Get Expenses List (with filters + pagination)
public record GetExpensesListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    Guid? CategoryId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    Guid? SubmittedById = null) : IRequest<PaginatedList<ExpenseListDto>>;

public record PaginatedList<T>(List<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public class GetExpensesListQueryHandler : IRequestHandler<GetExpensesListQuery, PaginatedList<ExpenseListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetExpensesListQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PaginatedList<ExpenseListDto>> Handle(GetExpensesListQuery request, CancellationToken ct)
    {
        var query = _context.Expenses
            .Include(e => e.Category)
            .Include(e => e.SubmittedBy)
            .AsNoTracking()
            .AsQueryable();

        // Employees see only their own expenses
        if (_currentUser.Role == UserRole.Employee.ToString())
            query = query.Where(e => e.SubmittedById == _currentUser.UserId);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<ExpenseStatus>(request.Status, out var status))
            query = query.Where(e => e.Status == status);

        if (request.CategoryId.HasValue)
            query = query.Where(e => e.CategoryId == request.CategoryId.Value);

        if (request.FromDate.HasValue)
            query = query.Where(e => e.ExpenseDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(e => e.ExpenseDate <= request.ToDate.Value);

        if (request.SubmittedById.HasValue)
            query = query.Where(e => e.SubmittedById == request.SubmittedById.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new ExpenseListDto(
                e.Id, e.Title, e.Amount, e.ExpenseDate,
                e.Status.ToString(),
                e.Category!.Name, e.Category.Color,
                e.SubmittedBy!.FirstName + " " + e.SubmittedBy.LastName,
                e.CreatedAt))
            .ToListAsync(ct);

        return new PaginatedList<ExpenseListDto>(items, totalCount, request.Page, request.PageSize);
    }
}

// Get Expense By Id
public record GetExpenseByIdQuery(Guid Id) : IRequest<ExpenseDto>;

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