using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUser;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUser) : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
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
            entity.Property(i => i.Amount).HasPrecision(18, 2);
            entity.HasQueryFilter(i => i.TenantId == _currentUser.TenantId);
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

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = _currentUser.Email;
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedBy = _currentUser.Email;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}