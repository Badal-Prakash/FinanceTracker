using FinanceTracker.Domain.Common;

namespace FinanceTracker.Domain.Entities;

public enum NotificationType
{
    ExpenseSubmitted = 1,
    ExpenseApproved = 2,
    ExpenseRejected = 3,
    InvoiceOverdue = 4,
    InvoicePaid = 5,
}

public class Notification : BaseEntity
{
    public Guid RecipientId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public bool IsRead { get; private set; }
    public Guid? EntityId { get; private set; } // e.g. ExpenseId or InvoiceId
    public string? EntityType { get; private set; } // "Expense" | "Invoice"

    private Notification() { }

    public static Notification Create(
        Guid recipientId,
        string title,
        string message,
        NotificationType type,
        Guid tenantId,
        Guid? entityId = null,
        string? entityType = null)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RecipientId = recipientId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            EntityId = entityId,
            EntityType = entityType
        };
    }

    public void MarkAsRead() => IsRead = true;
}