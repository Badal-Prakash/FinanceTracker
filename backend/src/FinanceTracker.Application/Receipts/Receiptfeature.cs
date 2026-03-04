using FinanceTracker.Application.Common.Exceptions;
using FinanceTracker.Application.Common.Interfaces;
using FinanceTracker.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Application.Receipts;

// ─── Allowed file types & size limits ─────────────────────────────────────────
public static class ReceiptPolicy
{
    public static readonly string[] AllowedMimeTypes =
    [
        "image/jpeg", "image/png", "image/webp", "image/heic",
        "application/pdf"
    ];

    public static readonly string[] AllowedExtensions =
        [".jpg", ".jpeg", ".png", ".webp", ".heic", ".pdf"];

    public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    public const string ContainerName = "receipts";
}

public record ReceiptUploadResultDto(string Url, string FileName, long SizeBytes);

public record UploadReceiptCommand(
    Guid ExpenseId,
    IFormFile File) : IRequest<ReceiptUploadResultDto>;

public class UploadReceiptCommandHandler
    : IRequestHandler<UploadReceiptCommand, ReceiptUploadResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IBlobStorageService _blob;
    private readonly ICurrentUserService _currentUser;

    public UploadReceiptCommandHandler(
        IApplicationDbContext context,
        IBlobStorageService blob,
        ICurrentUserService currentUser)
    {
        _context = context;
        _blob = blob;
        _currentUser = currentUser;
    }

    public async Task<ReceiptUploadResultDto> Handle(
        UploadReceiptCommand request, CancellationToken ct)
    {
        var file = request.File;

        if (file is null || file.Length == 0)
            throw new InvalidOperationException("No file was provided.");

        if (file.Length > ReceiptPolicy.MaxFileSizeBytes)
            throw new InvalidOperationException(
                $"File exceeds the maximum allowed size of 10 MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!ReceiptPolicy.AllowedExtensions.Contains(ext))
            throw new InvalidOperationException(
                $"File type '{ext}' is not allowed. Accepted: jpg, png, webp, heic, pdf.");

        if (!ReceiptPolicy.AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            throw new InvalidOperationException(
                $"MIME type '{file.ContentType}' is not allowed.");

        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseId, ct)
            ?? throw new NotFoundException(nameof(Expense), request.ExpenseId);

        var isOwner = expense.SubmittedById == _currentUser.UserId;
        var isAdmin = _currentUser.Role is "Admin" or "SuperAdmin";
        if (!isOwner && !isAdmin)
            throw new ForbiddenException(
                "You do not have permission to upload a receipt for this expense.");

        var safeOriginal = Path.GetFileNameWithoutExtension(file.FileName)
            .Replace(" ", "_")
            .Replace("..", "")
            [..Math.Min(40, Path.GetFileNameWithoutExtension(file.FileName).Length)];

        var uniqueName = $"{_currentUser.TenantId}/{request.ExpenseId}" +
                         $"/{DateTime.UtcNow:yyyyMMdd_HHmmss}_{safeOriginal}{ext}";

        await using var stream = file.OpenReadStream();
        var url = await _blob.UploadFileAsync(
            stream, uniqueName, ReceiptPolicy.ContainerName);

        // ── Attach URL to expense ─────────────────────────────────────────────
        expense.AttachReceipt(url);
        await _context.SaveChangesAsync(ct);

        return new ReceiptUploadResultDto(url, file.FileName, file.Length);
    }
}

// ─── COMMAND: Remove receipt from expense ────────────────────────────────────
public record RemoveReceiptCommand(Guid ExpenseId) : IRequest;

public class RemoveReceiptCommandHandler : IRequestHandler<RemoveReceiptCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IBlobStorageService _blob;
    private readonly ICurrentUserService _currentUser;

    public RemoveReceiptCommandHandler(
        IApplicationDbContext context,
        IBlobStorageService blob,
        ICurrentUserService currentUser)
    {
        _context = context;
        _blob = blob;
        _currentUser = currentUser;
    }

    public async Task Handle(RemoveReceiptCommand request, CancellationToken ct)
    {
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseId, ct)
            ?? throw new NotFoundException(nameof(Expense), request.ExpenseId);

        var isOwner = expense.SubmittedById == _currentUser.UserId;
        var isAdmin = _currentUser.Role is "Admin" or "SuperAdmin";
        if (!isOwner && !isAdmin)
            throw new ForbiddenException(
                "You do not have permission to remove this receipt.");

        if (string.IsNullOrEmpty(expense.ReceiptUrl))
            throw new InvalidOperationException("No receipt attached to this expense.");

        // Delete from blob storage
        await _blob.DeleteFileAsync(expense.ReceiptUrl);

        // Detach from expense
        expense.AttachReceipt(null!);
        await _context.SaveChangesAsync(ct);
    }
}