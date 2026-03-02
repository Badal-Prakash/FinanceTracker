using FinanceTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;        // ← this was missing
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;

namespace FinanceTracker.Application.Common.Interfaces;

// ─── Database Context ────────────────────────────────────────────────────────
public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLineItem> InvoiceLineItems { get; }
    DbSet<Budget> Budgets { get; }
    DbSet<Category> Categories { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

// ─── Current User ────────────────────────────────────────────────────────────
public interface ICurrentUserService
{
    Guid UserId { get; }
    Guid TenantId { get; }
    string Email { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
}

// ─── JWT Token ───────────────────────────────────────────────────────────────
public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid? GetUserIdFromExpiredToken(string token);
}

// ─── Email Service ───────────────────────────────────────────────────────────
public interface IEmailService
{
    Task SendExpenseApprovedEmailAsync(string toEmail, string expenseTitle, decimal amount);
    Task SendExpenseRejectedEmailAsync(string toEmail, string expenseTitle, string reason);
    Task SendInvoiceEmailAsync(string toEmail, string clientName, string invoiceNumber, decimal amount, DateTime dueDate);
}

// ─── Blob Storage ────────────────────────────────────────────────────────────
public interface IBlobStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName);
    Task DeleteFileAsync(string fileUrl);
}