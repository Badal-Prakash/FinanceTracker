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
// public class Invoice : BaseEntity
// {
//     public string InvoiceNumber { get; private set; } = string.Empty;
//     public string ClientName { get; private set; } = string.Empty;
//     public string ClientEmail { get; private set; } = string.Empty;
//     public string? ClientAddress { get; private set; }
//     public decimal Amount { get; private set; }
//     public DateTime DueDate { get; private set; }
//     public InvoiceStatus Status { get; private set; } = InvoiceStatus.Unpaid;
//     public DateTime? PaidAt { get; private set; }
//     public string? Notes { get; private set; }
//     public string? PdfUrl { get; private set; }

//     private Invoice() { }
//     // Add inside the Invoice class, after existing properties:
//     public List<InvoiceLineItem> LineItems { get; private set; } = new();

//     // Computed total from line items (if any), otherwise use Amount field
//     public decimal ComputedTotal => LineItems.Any()
//         ? LineItems.Sum(li => li.Total)
//         : Amount;

//     public static Invoice Create(string clientName, string clientEmail, decimal amount,
//         DateTime dueDate, Guid tenantId, string? clientAddress = null, string? notes = null)
//     {
//         return new Invoice
//         {
//             Id = Guid.NewGuid(),
//             TenantId = tenantId,
//             InvoiceNumber = GenerateInvoiceNumber(),
//             ClientName = clientName,
//             ClientEmail = clientEmail,
//             ClientAddress = clientAddress,
//             Amount = amount,
//             DueDate = dueDate,
//             Notes = notes,
//             Status = InvoiceStatus.Unpaid
//         };
//     }

//     public void MarkAsPaid()
//     {
//         if (Status == InvoiceStatus.Paid)
//             throw new InvalidOperationException("Invoice is already paid.");

//         Status = InvoiceStatus.Paid;
//         PaidAt = DateTime.UtcNow;
//     }

//     public void MarkAsOverdue()
//     {
//         if (Status == InvoiceStatus.Unpaid && DueDate < DateTime.UtcNow)
//             Status = InvoiceStatus.Overdue;
//     }

//     public void Cancel() => Status = InvoiceStatus.Cancelled;

//     public void AttachPdf(string pdfUrl) => PdfUrl = pdfUrl;

//     private static string GenerateInvoiceNumber()
//         => $"INV-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
// }

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; private set; } = string.Empty;
    public string ClientName { get; private set; } = string.Empty;
    public string ClientEmail { get; private set; } = string.Empty;
    public string? ClientAddress { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime DueDate { get; private set; }
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Draft;
    public DateTime? PaidAt { get; private set; }
    public string? Notes { get; private set; }
    public string? PdfUrl { get; private set; }

    public List<InvoiceLineItem> LineItems { get; private set; } = new();
    public decimal ComputedTotal => LineItems.Any() ? LineItems.Sum(li => li.Total) : Amount;

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
            Status = InvoiceStatus.Draft
        };
    }

    public void AddLineItem(InvoiceLineItem item) => LineItems.Add(item);

    public void ReplaceLineItems(List<InvoiceLineItem> items)
    {
        LineItems.Clear();
        LineItems.AddRange(items);
    }

    public void Update(string clientName, string clientEmail, string? clientAddress,
        DateTime dueDate, decimal amount, string? notes)
    {
        if (Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot edit a paid invoice.");

        ClientName = clientName;
        ClientEmail = clientEmail;
        ClientAddress = clientAddress;
        DueDate = dueDate;
        Amount = amount;
        Notes = notes;
    }

    public void MarkAsSent()
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be sent.");
        Status = InvoiceStatus.Unpaid;
    }

    public void MarkAsPaid()
    {
        if (Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Invoice is already paid.");
        if (Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException("Cannot mark a cancelled invoice as paid.");

        Status = InvoiceStatus.Paid;
        PaidAt = DateTime.UtcNow;
    }

    public void MarkAsOverdue()
    {
        if (Status == InvoiceStatus.Unpaid && DueDate < DateTime.UtcNow)
            Status = InvoiceStatus.Overdue;
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot cancel a paid invoice.");
        Status = InvoiceStatus.Cancelled;
    }

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

// ─── InvoiceLineItem ──────────────────────────────────────────────────────────
public class InvoiceLineItem
{
    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Total => Quantity * UnitPrice;

    private InvoiceLineItem() { }

    public static InvoiceLineItem Create(Guid invoiceId, string description, int quantity, decimal unitPrice)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive.");
        if (unitPrice < 0) throw new ArgumentException("Unit price cannot be negative.");

        return new InvoiceLineItem
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Description = description,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    public void Update(string description, int quantity, decimal unitPrice)
    {
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}