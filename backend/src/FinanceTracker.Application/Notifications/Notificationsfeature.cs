using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Notifications;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    Guid? EntityId,
    string? EntityType,
    DateTime CreatedAt);

public record NotificationSummaryDto(
    int UnreadCount,
    List<NotificationDto> Recent); // last 10

// ─── QUERIES ──────────────────────────────────────────────────────────────────

public record GetNotificationsQuery(
    bool UnreadOnly = false,
    int Page = 1,
    int PageSize = 20) : IRequest<List<NotificationDto>>;

public class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, List<NotificationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationsQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task<List<NotificationDto>> Handle(
        GetNotificationsQuery request, CancellationToken ct)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientId == _currentUser.UserId);

        if (request.UnreadOnly)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.Message,
                n.Type.ToString(), n.IsRead,
                n.EntityId, n.EntityType, n.CreatedAt))
            .ToListAsync(ct);
    }
}

public record GetNotificationSummaryQuery : IRequest<NotificationSummaryDto>;

public class GetNotificationSummaryQueryHandler
    : IRequestHandler<GetNotificationSummaryQuery, NotificationSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationSummaryQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task<NotificationSummaryDto> Handle(
        GetNotificationSummaryQuery request, CancellationToken ct)
    {
        var all = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientId == _currentUser.UserId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.Message,
                n.Type.ToString(), n.IsRead,
                n.EntityId, n.EntityType, n.CreatedAt))
            .Take(50) // enough to compute unread + get recent 10
            .ToListAsync(ct);

        return new NotificationSummaryDto(
            all.Count(n => !n.IsRead),
            all.Take(10).ToList());
    }
}

// ─── COMMANDS ─────────────────────────────────────────────────────────────────

public record MarkNotificationReadCommand(Guid NotificationId) : IRequest;

public class MarkNotificationReadCommandHandler
    : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public MarkNotificationReadCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.Id == request.NotificationId &&
                n.RecipientId == _currentUser.UserId, ct)
            ?? throw new KeyNotFoundException("Notification not found.");

        notification.MarkAsRead();
        await _context.SaveChangesAsync(ct);
    }
}

public record MarkAllNotificationsReadCommand : IRequest;

public class MarkAllNotificationsReadCommandHandler
    : IRequestHandler<MarkAllNotificationsReadCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public MarkAllNotificationsReadCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
    {
        var unread = await _context.Notifications
            .Where(n => n.RecipientId == _currentUser.UserId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var n in unread) n.MarkAsRead();
        await _context.SaveChangesAsync(ct);
    }
}

public record DeleteNotificationCommand(Guid NotificationId) : IRequest;

public class DeleteNotificationCommandHandler
    : IRequestHandler<DeleteNotificationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteNotificationCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    { _context = context; _currentUser = currentUser; }

    public async Task Handle(DeleteNotificationCommand request, CancellationToken ct)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.Id == request.NotificationId &&
                n.RecipientId == _currentUser.UserId, ct)
            ?? throw new KeyNotFoundException("Notification not found.");

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync(ct);
    }
}

// ─── DOMAIN EVENT HANDLERS ────────────────────────────────────────────────────

// Expense Submitted → notify all Managers and Admins in the tenant
public class ExpenseSubmittedNotificationHandler
    : INotificationHandler<ExpenseSubmittedEvent>
{
    private readonly IApplicationDbContext _context;

    public ExpenseSubmittedNotificationHandler(IApplicationDbContext context)
        => _context = context;

    public async Task Handle(ExpenseSubmittedEvent notification, CancellationToken ct)
    {
        var expense = notification.Expense;

        // Find all managers/admins in the same tenant
        var managers = await _context.Users
            .Where(u => u.TenantId == expense.TenantId &&
                        u.IsActive &&
                        (u.Role == UserRole.Manager ||
                         u.Role == UserRole.Admin ||
                         u.Role == UserRole.SuperAdmin) &&
                        u.Id != expense.SubmittedById)
            .Select(u => u.Id)
            .ToListAsync(ct);

        var submitterName = await _context.Users
            .Where(u => u.Id == expense.SubmittedById)
            .Select(u => u.FirstName + " " + u.LastName)
            .FirstOrDefaultAsync(ct) ?? "Someone";

        foreach (var managerId in managers)
        {
            _context.Notifications.Add(Notification.Create(
                managerId,
                "New Expense Submitted",
                $"{submitterName} submitted \"{expense.Title}\" for {expense.Amount:C} — awaiting your approval.",
                NotificationType.ExpenseSubmitted,
                expense.TenantId,
                expense.Id,
                "Expense"));
        }

        await _context.SaveChangesAsync(ct);
    }
}

// Expense Approved → notify the submitter
public class ExpenseApprovedNotificationHandler
    : INotificationHandler<ExpenseApprovedEvent>
{
    private readonly IApplicationDbContext _context;

    public ExpenseApprovedNotificationHandler(IApplicationDbContext context)
        => _context = context;

    public async Task Handle(ExpenseApprovedEvent notification, CancellationToken ct)
    {
        var expense = notification.Expense;

        _context.Notifications.Add(Notification.Create(
            expense.SubmittedById,
            "Expense Approved ✓",
            $"Your expense \"{expense.Title}\" ({expense.Amount:C}) has been approved.",
            NotificationType.ExpenseApproved,
            expense.TenantId,
            expense.Id,
            "Expense"));

        await _context.SaveChangesAsync(ct);
    }
}

// Expense Rejected → notify the submitter with reason
public class ExpenseRejectedNotificationHandler
    : INotificationHandler<ExpenseRejectedEvent>
{
    private readonly IApplicationDbContext _context;

    public ExpenseRejectedNotificationHandler(IApplicationDbContext context)
        => _context = context;

    public async Task Handle(ExpenseRejectedEvent notification, CancellationToken ct)
    {
        var expense = notification.Expense;

        var reason = string.IsNullOrEmpty(expense.RejectionReason)
            ? "No reason provided."
            : expense.RejectionReason;

        _context.Notifications.Add(Notification.Create(
            expense.SubmittedById,
            "Expense Rejected",
            $"Your expense \"{expense.Title}\" ({expense.Amount:C}) was rejected. Reason: {reason}",
            NotificationType.ExpenseRejected,
            expense.TenantId,
            expense.Id,
            "Expense"));

        await _context.SaveChangesAsync(ct);
    }
}

// Invoice Paid → notify the tenant's admins/superadmins
public class InvoicePaidNotificationHandler
    : INotificationHandler<InvoicePaidEvent>
{
    private readonly IApplicationDbContext _context;

    public InvoicePaidNotificationHandler(IApplicationDbContext context)
        => _context = context;

    public async Task Handle(InvoicePaidEvent notification, CancellationToken ct)
    {
        var invoice = notification.Invoice;

        // Notify all admins in the tenant
        var admins = await _context.Users
            .Where(u => u.TenantId == invoice.TenantId &&
                        u.IsActive &&
                        (u.Role == UserRole.Admin ||
                         u.Role == UserRole.SuperAdmin))
            .Select(u => u.Id)
            .ToListAsync(ct);

        foreach (var adminId in admins)
        {
            _context.Notifications.Add(Notification.Create(
                adminId,
                "Invoice Paid 💰",
                $"Invoice {invoice.InvoiceNumber} from {invoice.ClientName} ({invoice.Amount:C}) has been marked as paid.",
                NotificationType.InvoicePaid,
                invoice.TenantId,
                invoice.Id,
                "Invoice"));
        }

        await _context.SaveChangesAsync(ct);
    }
}

// Invoice Overdue → notify all admins in the tenant
public class InvoiceOverdueNotificationHandler
    : INotificationHandler<InvoiceOverdueEvent>
{
    private readonly IApplicationDbContext _context;

    public InvoiceOverdueNotificationHandler(IApplicationDbContext context)
        => _context = context;

    public async Task Handle(InvoiceOverdueEvent notification, CancellationToken ct)
    {
        var invoice = notification.Invoice;

        var admins = await _context.Users
            .Where(u => u.TenantId == invoice.TenantId &&
                        u.IsActive &&
                        (u.Role == UserRole.Admin ||
                         u.Role == UserRole.SuperAdmin))
            .Select(u => u.Id)
            .ToListAsync(ct);

        foreach (var adminId in admins)
        {
            _context.Notifications.Add(Notification.Create(
                adminId,
                "Invoice Overdue ⏰",
                $"Invoice {invoice.InvoiceNumber} for {invoice.ClientName} ({invoice.Amount:C}) was due on {invoice.DueDate:MMM dd, yyyy} and is now overdue.",
                NotificationType.InvoiceOverdue,
                invoice.TenantId,
                invoice.Id,
                "Invoice"));
        }

        await _context.SaveChangesAsync(ct);
    }
}