using FinanceTracker.Application.Common.Exceptions;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Invoices;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record InvoiceLineItemDto(
    Guid Id,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal Total);

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    string ClientName,
    string ClientEmail,
    string? ClientAddress,
    decimal Amount,
    DateTime DueDate,
    string Status,
    DateTime? PaidAt,
    string? Notes,
    string? PdfUrl,
    List<InvoiceLineItemDto> LineItems,
    DateTime CreatedAt);

public record InvoiceListDto(
    Guid Id,
    string InvoiceNumber,
    string ClientName,
    decimal Amount,
    DateTime DueDate,
    string Status,
    DateTime? PaidAt,
    DateTime CreatedAt);

public record CreateLineItemRequest(
    string Description,
    int Quantity,
    decimal UnitPrice);

// ─── QUERIES ──────────────────────────────────────────────────────────────────

public record GetInvoicesListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    string? ClientName = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<PaginatedInvoiceList>;

public record PaginatedInvoiceList(
    List<InvoiceListDto> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public class GetInvoicesListQueryHandler : IRequestHandler<GetInvoicesListQuery, PaginatedInvoiceList>
{
    private readonly IApplicationDbContext _context;

    public GetInvoicesListQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<PaginatedInvoiceList> Handle(GetInvoicesListQuery request, CancellationToken ct)
    {
        var query = _context.Invoices.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<InvoiceStatus>(request.Status, out var status))
            query = query.Where(i => i.Status == status);

        if (!string.IsNullOrWhiteSpace(request.ClientName))
            query = query.Where(i => i.ClientName.ToLower().Contains(request.ClientName.ToLower()));

        if (request.FromDate.HasValue)
            query = query.Where(i => i.DueDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(i => i.DueDate <= request.ToDate.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new InvoiceListDto(
                i.Id,
                i.InvoiceNumber,
                i.ClientName,
                i.Amount,
                i.DueDate,
                i.Status.ToString(),
                i.PaidAt,
                i.CreatedAt))
            .ToListAsync(ct);

        return new PaginatedInvoiceList(items, totalCount, request.Page, request.PageSize);
    }
}

public record GetInvoiceByIdQuery(Guid Id) : IRequest<InvoiceDto>;

public class GetInvoiceByIdQueryHandler : IRequestHandler<GetInvoiceByIdQuery, InvoiceDto>
{
    private readonly IApplicationDbContext _context;

    public GetInvoiceByIdQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<InvoiceDto> Handle(GetInvoiceByIdQuery request, CancellationToken ct)
    {
        var i = await _context.Invoices
            .Include(x => x.LineItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Invoice), request.Id);

        return MapToDto(i);
    }

    public static InvoiceDto MapToDto(Invoice i) => new(
        i.Id,
        i.InvoiceNumber,
        i.ClientName,
        i.ClientEmail,
        i.ClientAddress,
        i.ComputedTotal,
        i.DueDate,
        i.Status.ToString(),
        i.PaidAt,
        i.Notes,
        i.PdfUrl,
        i.LineItems.Select(li => new InvoiceLineItemDto(
            li.Id, li.Description, li.Quantity, li.UnitPrice, li.Total)).ToList(),
        i.CreatedAt);
}

// ─── COMMANDS ─────────────────────────────────────────────────────────────────

// Create Invoice
public record CreateInvoiceCommand(
    string ClientName,
    string ClientEmail,
    string? ClientAddress,
    DateTime DueDate,
    string? Notes,
    List<CreateLineItemRequest> LineItems) : IRequest<Guid>;

public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.ClientName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ClientEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.DueDate).GreaterThan(DateTime.UtcNow.AddDays(-1))
            .WithMessage("Due date must be today or in the future.");
        RuleFor(x => x.LineItems).NotEmpty()
            .WithMessage("At least one line item is required.");
        RuleForEach(x => x.LineItems).ChildRules(item =>
        {
            item.RuleFor(li => li.Description).NotEmpty().MaximumLength(300);
            item.RuleFor(li => li.Quantity).GreaterThan(0);
            item.RuleFor(li => li.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateInvoiceCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateInvoiceCommand request, CancellationToken ct)
    {
        // Compute total from line items
        var total = request.LineItems.Sum(li => li.Quantity * li.UnitPrice);

        var invoice = Invoice.Create(
            request.ClientName,
            request.ClientEmail,
            total,
            request.DueDate,
            _currentUser.TenantId,
            request.ClientAddress,
            request.Notes);

        // Add line items
        foreach (var li in request.LineItems)
        {
            var lineItem = InvoiceLineItem.Create(invoice.Id, li.Description, li.Quantity, li.UnitPrice);
            invoice.AddLineItem(lineItem);
        }

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(ct);
        return invoice.Id;
    }
}

// Update Invoice
public record UpdateInvoiceCommand(
    Guid Id,
    string ClientName,
    string ClientEmail,
    string? ClientAddress,
    DateTime DueDate,
    string? Notes,
    List<CreateLineItemRequest> LineItems) : IRequest;

public class UpdateInvoiceCommandValidator : AbstractValidator<UpdateInvoiceCommand>
{
    public UpdateInvoiceCommandValidator()
    {
        RuleFor(x => x.ClientName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ClientEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.LineItems).NotEmpty()
            .WithMessage("At least one line item is required.");
        RuleForEach(x => x.LineItems).ChildRules(item =>
        {
            item.RuleFor(li => li.Description).NotEmpty().MaximumLength(300);
            item.RuleFor(li => li.Quantity).GreaterThan(0);
            item.RuleFor(li => li.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

public class UpdateInvoiceCommandHandler : IRequestHandler<UpdateInvoiceCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateInvoiceCommandHandler(IApplicationDbContext context)
        => _context = context;

    public async Task Handle(UpdateInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await _context.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Invoice), request.Id);

        var total = request.LineItems.Sum(li => li.Quantity * li.UnitPrice);

        invoice.Update(
            request.ClientName,
            request.ClientEmail,
            request.ClientAddress,
            request.DueDate,
            total,
            request.Notes);

        // Replace line items
        invoice.ReplaceLineItems(
            request.LineItems.Select(li =>
                InvoiceLineItem.Create(invoice.Id, li.Description, li.Quantity, li.UnitPrice))
            .ToList());

        await _context.SaveChangesAsync(ct);
    }
}

// Mark as Paid
public record MarkInvoicePaidCommand(Guid Id) : IRequest;

public class MarkInvoicePaidCommandHandler : IRequestHandler<MarkInvoicePaidCommand>
{
    private readonly IApplicationDbContext _context;

    public MarkInvoicePaidCommandHandler(IApplicationDbContext context)
        => _context = context;

    public async Task Handle(MarkInvoicePaidCommand request, CancellationToken ct)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Invoice), request.Id);

        invoice.MarkAsPaid();
        await _context.SaveChangesAsync(ct);
    }
}

// Cancel Invoice
public record CancelInvoiceCommand(Guid Id) : IRequest;

public class CancelInvoiceCommandHandler : IRequestHandler<CancelInvoiceCommand>
{
    private readonly IApplicationDbContext _context;

    public CancelInvoiceCommandHandler(IApplicationDbContext context)
        => _context = context;

    public async Task Handle(CancelInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Invoice), request.Id);

        invoice.Cancel();
        await _context.SaveChangesAsync(ct);
    }
}

// Delete Invoice
public record DeleteInvoiceCommand(Guid Id) : IRequest;

public class DeleteInvoiceCommandHandler : IRequestHandler<DeleteInvoiceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteInvoiceCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteInvoiceCommand request, CancellationToken ct)
    {
        if (_currentUser.Role == UserRole.Employee.ToString())
            throw new ForbiddenException("Only managers and admins can delete invoices.");

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Invoice), request.Id);

        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Paid invoices cannot be deleted.");

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync(ct);
    }
}

// Send Invoice (mark as sent — future: trigger email)
public record SendInvoiceCommand(Guid Id) : IRequest;

public class SendInvoiceCommandHandler : IRequestHandler<SendInvoiceCommand>
{
    private readonly IApplicationDbContext _context;

    public SendInvoiceCommandHandler(IApplicationDbContext context)
        => _context = context;

    public async Task Handle(SendInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Invoice), request.Id);

        invoice.MarkAsSent();
        await _context.SaveChangesAsync(ct);
    }
}

// Dashboard stats for invoices
public record GetInvoiceStatsQuery : IRequest<InvoiceStatsDto>;

public record InvoiceStatsDto(
    decimal TotalUnpaid,
    decimal TotalPaidThisMonth,
    decimal TotalOverdue,
    int UnpaidCount,
    int OverdueCount,
    int PaidThisMonthCount);

public class GetInvoiceStatsQueryHandler : IRequestHandler<GetInvoiceStatsQuery, InvoiceStatsDto>
{
    private readonly IApplicationDbContext _context;

    public GetInvoiceStatsQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<InvoiceStatsDto> Handle(GetInvoiceStatsQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var invoices = await _context.Invoices
            .AsNoTracking()
            .Select(i => new { i.Amount, i.Status, i.PaidAt, i.DueDate })
            .ToListAsync(ct);

        return new InvoiceStatsDto(
            TotalUnpaid: invoices.Where(i => i.Status == InvoiceStatus.Unpaid).Sum(i => i.Amount),
            TotalPaidThisMonth: invoices
                .Where(i => i.Status == InvoiceStatus.Paid && i.PaidAt >= startOfMonth)
                .Sum(i => i.Amount),
            TotalOverdue: invoices.Where(i => i.Status == InvoiceStatus.Overdue).Sum(i => i.Amount),
            UnpaidCount: invoices.Count(i => i.Status == InvoiceStatus.Unpaid),
            OverdueCount: invoices.Count(i => i.Status == InvoiceStatus.Overdue),
            PaidThisMonthCount: invoices.Count(i => i.Status == InvoiceStatus.Paid && i.PaidAt >= startOfMonth));
    }
}