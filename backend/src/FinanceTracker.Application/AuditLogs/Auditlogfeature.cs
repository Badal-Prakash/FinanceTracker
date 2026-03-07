using FinanceTracker.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.AuditLogs;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string UserEmail,
    string Action,
    string EntityName,
    Guid EntityId,
    string? OldValues,
    string? NewValues,
    string? ChangedFields,
    DateTime Timestamp,
    string? IpAddress);

public record AuditLogPageDto(
    List<AuditLogDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

// ─── QUERIES ──────────────────────────────────────────────────────────────────

public record GetAuditLogsQuery(
    string? EntityName = null,
    Guid? EntityId = null,
    Guid? UserId = null,
    string? Action = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 30) : IRequest<AuditLogPageDto>;

public class GetAuditLogsQueryHandler
    : IRequestHandler<GetAuditLogsQuery, AuditLogPageDto>
{
    private readonly IApplicationDbContext _context;

    public GetAuditLogsQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<AuditLogPageDto> Handle(
        GetAuditLogsQuery request, CancellationToken ct)
    {
        var query = _context.AuditLogs
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EntityName))
            query = query.Where(a => a.EntityName == request.EntityName);

        if (request.EntityId.HasValue)
            query = query.Where(a => a.EntityId == request.EntityId.Value);

        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId.Value);

        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(a => a.Action == request.Action);

        if (request.From.HasValue)
            query = query.Where(a => a.Timestamp >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(a => a.Timestamp <= request.To.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.UserId, a.UserEmail,
                a.Action, a.EntityName, a.EntityId,
                a.OldValues, a.NewValues, a.ChangedFields,
                a.Timestamp, a.IpAddress))
            .ToListAsync(ct);

        return new AuditLogPageDto(
            items, total, request.Page, request.PageSize,
            (int)Math.Ceiling(total / (double)request.PageSize));
    }
}

// Single entity's full history — e.g. all changes to Expense abc-123
public record GetEntityAuditHistoryQuery(
    string EntityName,
    Guid EntityId) : IRequest<List<AuditLogDto>>;

public class GetEntityAuditHistoryQueryHandler
    : IRequestHandler<GetEntityAuditHistoryQuery, List<AuditLogDto>>
{
    private readonly IApplicationDbContext _context;

    public GetEntityAuditHistoryQueryHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<List<AuditLogDto>> Handle(
        GetEntityAuditHistoryQuery request, CancellationToken ct)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityName == request.EntityName
                     && a.EntityId == request.EntityId)
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new AuditLogDto(
                a.Id, a.UserId, a.UserEmail,
                a.Action, a.EntityName, a.EntityId,
                a.OldValues, a.NewValues, a.ChangedFields,
                a.Timestamp, a.IpAddress))
            .ToListAsync(ct);
    }
}