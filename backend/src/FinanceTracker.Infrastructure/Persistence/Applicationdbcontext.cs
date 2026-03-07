using System.Text.Json;
using FinanceTracker.Application.Common.Interfaces;
using MediatR;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUser;

    private readonly IPublisher _mediator;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUser, IPublisher mediator) : base(options)
    {
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Tenant ────────────────────────────────────────────────────────
        builder.Entity<Tenant>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Subdomain).IsRequired().HasMaxLength(50);
            entity.HasIndex(t => t.Subdomain).IsUnique();
        });

        // ── User ──────────────────────────────────────────────────────────
        builder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasQueryFilter(u => u.TenantId == _currentUser.TenantId);
        });

        // ── Category ──────────────────────────────────────────────────────
        builder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Color).HasMaxLength(20);
            entity.Property(c => c.Icon).HasMaxLength(50);
            entity.HasQueryFilter(c => c.TenantId == _currentUser.TenantId);
        });

        // ── Expense ───────────────────────────────────────────────────────
        builder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasOne(e => e.Category)
                  .WithMany()
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.SubmittedBy)
                  .WithMany()
                  .HasForeignKey(e => e.SubmittedById)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Approver)
                  .WithMany()
                  .HasForeignKey(e => e.ApproverId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => e.TenantId == _currentUser.TenantId);
        });

        // ── Invoice ───────────────────────────────────────────────────────
        builder.Entity<Invoice>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
            entity.Property(i => i.ClientName).IsRequired().HasMaxLength(200);
            entity.Property(i => i.ClientEmail).IsRequired().HasMaxLength(200);
            entity.Property(i => i.ClientAddress).HasMaxLength(500);
            entity.Property(i => i.Amount).HasPrecision(18, 2);
            entity.HasQueryFilter(i => i.TenantId == _currentUser.TenantId);
            entity.HasMany(i => i.LineItems)
                  .WithOne(li => li.Invoice)
                  .HasForeignKey(li => li.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── InvoiceLineItem ───────────────────────────────────────────────
        builder.Entity<InvoiceLineItem>(entity =>
        {
            entity.HasKey(li => li.Id);
            entity.Property(li => li.Description).IsRequired().HasMaxLength(500);
            entity.Property(li => li.UnitPrice).HasPrecision(18, 2);
            entity.Ignore(li => li.Total); // computed property
            entity.HasQueryFilter(li => li.TenantId == _currentUser.TenantId);
        });

        // ── Notification ──────────────────────────────────────────────────────
        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Title).IsRequired().HasMaxLength(200);
            entity.Property(n => n.Message).IsRequired().HasMaxLength(1000);
            entity.HasQueryFilter(n => n.TenantId == _currentUser.TenantId);
        });

        // ── AuditLog ───────────────────────────────────────────────────────────
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.UserEmail).IsRequired().HasMaxLength(200);
            entity.Property(a => a.Action).IsRequired().HasMaxLength(50);
            entity.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
            entity.Property(a => a.ChangedFields).HasMaxLength(2000);
            entity.Property(a => a.IpAddress).HasMaxLength(50);
            // OldValues / NewValues are JSON — no max length (stored as text)
            entity.HasIndex(a => new { a.TenantId, a.Timestamp });
            entity.HasIndex(a => new { a.EntityName, a.EntityId });
            entity.HasQueryFilter(a => a.TenantId == _currentUser.TenantId);
        });

        // ── Budget ────────────────────────────────────────────────────────
        builder.Entity<Budget>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Amount).HasPrecision(18, 2);
            entity.HasOne(b => b.Category)
                  .WithMany()
                  .HasForeignKey(b => b.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(b => b.TenantId == _currentUser.TenantId);
        });
    }

    // Entities to skip from audit log (noisy / internal)
    private static readonly HashSet<string> _auditExclusions = new()
    {
        nameof(AuditLog),
        nameof(Notification),
    };

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit stamps — guard against null (background service has no HTTP context)
        var actor = _currentUser.IsAuthenticated ? _currentUser.Email : "system";
        var actorId = _currentUser.IsAuthenticated ? _currentUser.UserId : (Guid?)null;
        var tenantId = _currentUser.TenantId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = actor;
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedBy = actor;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        // ── Capture audit entries BEFORE saving so OldValues are available ──
        var auditEntries = BuildAuditEntries(actor, actorId, tenantId);

        // ── Collect and clear domain events before saving ──────────────────
        var events = ChangeTracker.Entries<BaseEntity>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            entry.Entity.ClearDomainEvents();

        // ── Persist changes ────────────────────────────────────────────────
        var result = await base.SaveChangesAsync(cancellationToken);

        // ── Persist audit logs (after save so Ids are populated for Added) ─
        if (auditEntries.Count > 0)
        {
            AuditLogs.AddRange(auditEntries);
            await base.SaveChangesAsync(cancellationToken);
        }

        // ── Dispatch domain events ─────────────────────────────────────────
        foreach (var domainEvent in events)
            await _mediator.Publish((INotification)domainEvent, cancellationToken);

        return result;
    }

    private List<AuditLog> BuildAuditEntries(
        string actor, Guid? actorId, Guid tenantId)
    {
        var logs = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            // Skip excluded types and unchanged entries
            var entityName = entry.Entity.GetType().Name;
            if (_auditExclusions.Contains(entityName)) continue;
            if (entry.State is not (EntityState.Added
                                 or EntityState.Modified
                                 or EntityState.Deleted)) continue;

            var entityId = entry.Entity.Id;
            var entryTenantId = entry.Entity.TenantId != Guid.Empty
                ? entry.Entity.TenantId : tenantId;

            string action;
            string? oldJson = null;
            string? newJson = null;
            string? changedFields = null;

            switch (entry.State)
            {
                case EntityState.Added:
                    action = "Created";
                    newJson = JsonSerializer.Serialize(
                        entry.CurrentValues.ToObject(),
                        new JsonSerializerOptions { WriteIndented = false });
                    break;

                case EntityState.Deleted:
                    action = "Deleted";
                    oldJson = JsonSerializer.Serialize(
                        entry.OriginalValues.ToObject(),
                        new JsonSerializerOptions { WriteIndented = false });
                    break;

                default: // Modified
                    action = "Updated";
                    var changed = entry.Properties
                        .Where(p => p.IsModified
                                 && !new[] { "UpdatedAt", "UpdatedBy" }.Contains(p.Metadata.Name))
                        .ToList();

                    if (!changed.Any()) continue; // skip if only audit stamps changed

                    changedFields = string.Join(", ", changed.Select(p => p.Metadata.Name));
                    oldJson = JsonSerializer.Serialize(
                        changed.ToDictionary(
                            p => p.Metadata.Name,
                            p => p.OriginalValue),
                        new JsonSerializerOptions { WriteIndented = false });
                    newJson = JsonSerializer.Serialize(
                        changed.ToDictionary(
                            p => p.Metadata.Name,
                            p => p.CurrentValue),
                        new JsonSerializerOptions { WriteIndented = false });
                    break;
            }

            logs.Add(AuditLog.Create(
                entryTenantId, actorId, actor,
                action, entityName, entityId,
                oldJson, newJson, changedFields));
        }

        return logs;
    }
}