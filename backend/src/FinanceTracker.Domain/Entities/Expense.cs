using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Domain.Events;

namespace FinanceTracker.Domain.Entities;

public class Expense : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime ExpenseDate { get; private set; }
    public ExpenseStatus Status { get; private set; } = ExpenseStatus.Draft;
    public string? ReceiptUrl { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid SubmittedById { get; private set; }
    public Guid? ApproverId { get; private set; }
    public Guid CategoryId { get; private set; }

    // Navigation properties
    public User? SubmittedBy { get; private set; }
    public User? Approver { get; private set; }
    public Category? Category { get; private set; }

    private Expense() { }

    public static Expense Create(string title, string? description, decimal amount,
        DateTime expenseDate, Guid categoryId, Guid submittedById, Guid tenantId,
        string? receiptUrl = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        return new Expense
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            Description = description,
            Amount = amount,
            ExpenseDate = expenseDate,
            CategoryId = categoryId,
            SubmittedById = submittedById,
            Status = ExpenseStatus.Draft,
            ReceiptUrl = receiptUrl
        };
    }

    public void Submit()
    {
        if (Status != ExpenseStatus.Draft)
            throw new InvalidOperationException("Only draft expenses can be submitted.");

        Status = ExpenseStatus.Submitted;
        AddDomainEvent(new ExpenseSubmittedEvent(this));
    }

    public void Approve(Guid approverId)
    {
        if (Status != ExpenseStatus.Submitted)
            throw new InvalidOperationException("Only submitted expenses can be approved.");

        Status = ExpenseStatus.Approved;
        ApproverId = approverId;
        AddDomainEvent(new ExpenseApprovedEvent(this));
    }

    public void Reject(Guid approverId, string reason)
    {
        if (Status != ExpenseStatus.Submitted)
            throw new InvalidOperationException("Only submitted expenses can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));

        Status = ExpenseStatus.Rejected;
        ApproverId = approverId;
        RejectionReason = reason;
        AddDomainEvent(new ExpenseRejectedEvent(this));
    }

    public void AttachReceipt(string receiptUrl)
    {
        if (Status == ExpenseStatus.Approved)
            throw new InvalidOperationException("Cannot modify an approved expense.");

        ReceiptUrl = receiptUrl;
    }

    public void Update(string title, string? description, decimal amount,
        DateTime expenseDate, Guid categoryId)
    {
        if (Status != ExpenseStatus.Draft)
            throw new InvalidOperationException("Only draft expenses can be edited.");

        Title = title;
        Description = description;
        Amount = amount;
        ExpenseDate = expenseDate;
        CategoryId = categoryId;
    }
}