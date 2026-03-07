namespace FinanceTracker.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public string UserEmail { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty; // Created | Updated | Deleted
    public string EntityName { get; private set; } = string.Empty; // "Expense", "Invoice", etc.
    public Guid EntityId { get; private set; }
    public string? OldValues { get; private set; } // JSON snapshot before
    public string? NewValues { get; private set; } // JSON snapshot after
    public string? ChangedFields { get; private set; } // comma-separated list of changed props
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
    public string? IpAddress { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        Guid tenantId,
        Guid? userId,
        string userEmail,
        string action,
        string entityName,
        Guid entityId,
        string? oldValues = null,
        string? newValues = null,
        string? changedFields = null,
        string? ipAddress = null)
    {
        return new AuditLog
        {
            TenantId = tenantId,
            UserId = userId,
            UserEmail = userEmail,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            ChangedFields = changedFields,
            Timestamp = DateTime.UtcNow,
            IpAddress = ipAddress
        };
    }
}