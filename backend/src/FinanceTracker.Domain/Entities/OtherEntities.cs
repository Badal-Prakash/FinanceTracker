using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Domain.Entities;

// ─── Category ────────────────────────────────────────────────────────────────
public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Color { get; private set; } = "#6366f1"; // default indigo
    public string Icon { get; private set; } = "folder";
    public bool IsActive { get; private set; } = true;

    private Category() { }

    public static Category Create(string name, Guid tenantId,
        string color = "#6366f1", string icon = "folder")
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Color = color,
            Icon = icon
        };
    }

    public void Update(string name, string color, string icon)
    {
        Name = name;
        Color = color;
        Icon = icon;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

// ─── Invoice ─────────────────────────────────────────────────────────────────
public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; private set; } = string.Empty;
    public string ClientName { get; private set; } = string.Empty;
    public string ClientEmail { get; private set; } = string.Empty;
    public string? ClientAddress { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime DueDate { get; private set; }
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Unpaid;
    public DateTime? PaidAt { get; private set; }
    public string? Notes { get; private set; }
    public string? PdfUrl { get; private set; }

    private Invoice() { }

    public static Invoice Create(string clientName, string clientEmail, decimal amount,
        DateTime dueDate, Guid tenantId, string? clientAddress = null, string? notes = null)
    {
        return new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceNumber = GenerateInvoiceNumber(),
            ClientName = clientName,
            ClientEmail = clientEmail,
            ClientAddress = clientAddress,
            Amount = amount,
            DueDate = dueDate,
            Notes = notes,
            Status = InvoiceStatus.Unpaid
        };
    }

    public void MarkAsPaid()
    {
        if (Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Invoice is already paid.");

        Status = InvoiceStatus.Paid;
        PaidAt = DateTime.UtcNow;
    }

    public void MarkAsOverdue()
    {
        if (Status == InvoiceStatus.Unpaid && DueDate < DateTime.UtcNow)
            Status = InvoiceStatus.Overdue;
    }

    public void Cancel() => Status = InvoiceStatus.Cancelled;

    public void AttachPdf(string pdfUrl) => PdfUrl = pdfUrl;

    private static string GenerateInvoiceNumber()
        => $"INV-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
}

// ─── Budget ──────────────────────────────────────────────────────────────────
public class Budget : BaseEntity
{
    public Guid CategoryId { get; private set; }
    public decimal Amount { get; private set; }
    public int Month { get; private set; }  // 1-12
    public int Year { get; private set; }

    public Category? Category { get; private set; }

    private Budget() { }

    public static Budget Create(Guid categoryId, decimal amount,
        int month, int year, Guid tenantId)
    {
        if (amount <= 0) throw new ArgumentException("Budget amount must be positive.");
        if (month < 1 || month > 12) throw new ArgumentException("Month must be 1-12.");

        return new Budget
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CategoryId = categoryId,
            Amount = amount,
            Month = month,
            Year = year
        };
    }

    public void UpdateAmount(decimal newAmount)
    {
        if (newAmount <= 0) throw new ArgumentException("Budget amount must be positive.");
        Amount = newAmount;
    }
}