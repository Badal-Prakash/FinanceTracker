using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
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

public record InvoiceListDto(
    Guid Id,
    string InvoiceNumber,
    string ClientName,
    string ClientEmail,
    decimal Amount,
    DateTime DueDate,
    string Status,
    DateTime? PaidAt,
    DateTime CreatedAt);

public record InvoiceDetailDto(
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
    DateTime CreatedAt,
    List<InvoiceLineItemDto> LineItems);

public record InvoiceStatsDto(
    decimal TotalUnpaid,
    decimal TotalPaidThisMonth,
    decimal TotalOverdue,
    int UnpaidCount,
    int OverdueCount,
    int PaidThisMonthCount);

public record PaginatedList<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

// ─── QUERIES ──────────────────────────────────────────────────────────────────

public record GetInvoicesListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    string? ClientName = null,
    string? FromDate = null,
    string? ToDate = null)
    : IRequest<PaginatedList<InvoiceListDto>>;

public class GetInvoicesListQueryHandler
    : IRequestHandler<GetInvoicesListQuery, PaginatedList<InvoiceListDto>>
{
    private readonly IApplicationDbContext _context;

    public GetInvoicesListQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<PaginatedList<InvoiceListDto>> Handle(
        GetInvoicesListQuery request, CancellationToken ct)
    {
        var query = _context.Invoices.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(request.Status) &&
            Enum.TryParse<InvoiceStatus>(request.Status, out var statusEnum))
            query = query.Where(i => i.Status == statusEnum);

        if (!string.IsNullOrEmpty(request.ClientName))
            query = query.Where(i => i.ClientName.Contains(request.ClientName));

        if (!string.IsNullOrEmpty(request.FromDate) &&
            DateTime.TryParse(request.FromDate, out var from))
            query = query.Where(i => i.DueDate >= from);

        if (!string.IsNullOrEmpty(request.ToDate) &&
            DateTime.TryParse(request.ToDate, out var to))
            query = query.Where(i => i.DueDate <= to);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new InvoiceListDto(
                i.Id, i.InvoiceNumber, i.ClientName, i.ClientEmail,
                i.Amount, i.DueDate, i.Status.ToString(),
                i.PaidAt, i.CreatedAt))
            .ToListAsync(ct);

        return new PaginatedList<InvoiceListDto>(items, total, request.Page, request.PageSize);
    }
}

public record GetInvoiceByIdQuery(Guid Id) : IRequest<InvoiceDetailDto>;

public class GetInvoiceByIdQueryHandler
    : IRequestHandler<GetInvoiceByIdQuery, InvoiceDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetInvoiceByIdQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<InvoiceDetailDto> Handle(
        GetInvoiceByIdQuery request, CancellationToken ct)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"Invoice {request.Id} not found.");

        return new InvoiceDetailDto(
            invoice.Id, invoice.InvoiceNumber,
            invoice.ClientName, invoice.ClientEmail, invoice.ClientAddress,
            invoice.Amount, invoice.DueDate, invoice.Status.ToString(),
            invoice.PaidAt, invoice.Notes, invoice.PdfUrl, invoice.CreatedAt,
            invoice.LineItems.Select(li => new InvoiceLineItemDto(
                li.Id, li.Description, li.Quantity, li.UnitPrice, li.Total))
            .ToList());
    }
}

public record GetInvoiceStatsQuery : IRequest<InvoiceStatsDto>;

public class GetInvoiceStatsQueryHandler
    : IRequestHandler<GetInvoiceStatsQuery, InvoiceStatsDto>
{
    private readonly IApplicationDbContext _context;

    public GetInvoiceStatsQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<InvoiceStatsDto> Handle(
        GetInvoiceStatsQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        // Pull to memory to avoid EF translation issues
        var all = await _context.Invoices
            .AsNoTracking()
            .Select(i => new { i.Status, i.Amount, i.PaidAt })
            .ToListAsync(ct);

        var unpaid = all.Where(i => i.Status == InvoiceStatus.Unpaid).ToList();
        var overdue = all.Where(i => i.Status == InvoiceStatus.Overdue).ToList();
        var paidMonth = all.Where(i => i.Status == InvoiceStatus.Paid
                                   && i.PaidAt >= start && i.PaidAt < end).ToList();

        return new InvoiceStatsDto(
            unpaid.Sum(i => i.Amount),
            paidMonth.Sum(i => i.Amount),
            overdue.Sum(i => i.Amount),
            unpaid.Count,
            overdue.Count,
            paidMonth.Count);
    }
}

// ─── COMMANDS ─────────────────────────────────────────────────────────────────

public record LineItemRequest(string Description, int Quantity, decimal UnitPrice);

public record CreateInvoiceCommand(
    string ClientName,
    string ClientEmail,
    string? ClientAddress,
    DateTime DueDate,
    string? Notes,
    List<LineItemRequest> LineItems)
    : IRequest<Guid>;

public class CreateInvoiceCommandHandler
    : IRequestHandler<CreateInvoiceCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateInvoiceCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task<Guid> Handle(
        CreateInvoiceCommand request, CancellationToken ct)
    {
        if (!request.LineItems.Any())
            throw new ArgumentException("Invoice must have at least one line item.");

        var totalAmount = request.LineItems.Sum(li => li.Quantity * li.UnitPrice);

        var invoice = Invoice.Create(
            request.ClientName, request.ClientEmail, totalAmount,
            request.DueDate, _currentUser.TenantId,
            request.ClientAddress, request.Notes);

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(ct);

        // Add line items
        foreach (var li in request.LineItems)
        {
            var lineItem = InvoiceLineItem.Create(
                invoice.Id, li.Description, li.Quantity, li.UnitPrice,
                _currentUser.TenantId);
            _context.InvoiceLineItems.Add(lineItem);
        }

        await _context.SaveChangesAsync(ct);
        return invoice.Id;
    }
}

public record UpdateInvoiceCommand(
    Guid InvoiceId,
    string ClientName,
    string ClientEmail,
    string? ClientAddress,
    DateTime DueDate,
    string? Notes,
    List<LineItemRequest> LineItems)
    : IRequest;

public class UpdateInvoiceCommandHandler
    : IRequestHandler<UpdateInvoiceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateInvoiceCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task Handle(UpdateInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await _context.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        if (!request.LineItems.Any())
            throw new ArgumentException("Invoice must have at least one line item.");

        var totalAmount = request.LineItems.Sum(li => li.Quantity * li.UnitPrice);

        invoice.Update(request.ClientName, request.ClientEmail,
            request.DueDate, request.ClientAddress, request.Notes, totalAmount);

        // Replace line items
        _context.InvoiceLineItems.RemoveRange(invoice.LineItems);

        foreach (var li in request.LineItems)
        {
            _context.InvoiceLineItems.Add(InvoiceLineItem.Create(
                invoice.Id, li.Description, li.Quantity, li.UnitPrice,
                _currentUser.TenantId));
        }

        await _context.SaveChangesAsync(ct);
    }
}

public record MarkInvoicePaidCommand(Guid InvoiceId) : IRequest;

public class MarkInvoicePaidCommandHandler
    : IRequestHandler<MarkInvoicePaidCommand>
{
    private readonly IApplicationDbContext _context;

    public MarkInvoicePaidCommandHandler(IApplicationDbContext context)
        => _context = context;

    public async Task Handle(MarkInvoicePaidCommand request, CancellationToken ct)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        invoice.MarkAsPaid();
        await _context.SaveChangesAsync(ct);
    }
}

public record CancelInvoiceCommand(Guid InvoiceId) : IRequest;

public class CancelInvoiceCommandHandler
    : IRequestHandler<CancelInvoiceCommand>
{
    private readonly IApplicationDbContext _context;

    public CancelInvoiceCommandHandler(IApplicationDbContext context)
        => _context = context;

    public async Task Handle(CancelInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        invoice.Cancel();
        await _context.SaveChangesAsync(ct);
    }
}

public record DeleteInvoiceCommand(Guid InvoiceId) : IRequest;

public class DeleteInvoiceCommandHandler
    : IRequestHandler<DeleteInvoiceCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteInvoiceCommandHandler(IApplicationDbContext context)
        => _context = context;

    public async Task Handle(DeleteInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await _context.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct)
            ?? throw new KeyNotFoundException("Invoice not found.");

        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot delete a paid invoice.");

        _context.InvoiceLineItems.RemoveRange(invoice.LineItems);
        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync(ct);
    }
}