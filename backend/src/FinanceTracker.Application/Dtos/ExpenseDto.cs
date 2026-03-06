using System;

namespace FinanceTracker.Application.Dtos;

public record ExpenseDto(
    Guid Id,
    string Title,
    string? Description,
    decimal Amount,
    DateTime ExpenseDate,
    string Status,
    string? ReceiptUrl,
    string? RejectionReason,
    string SubmittedByName,
    string? ApproverName,
    string CategoryName,
    string CategoryColor,
    DateTime CreatedAt);

public record ExpenseListDto(
    Guid Id,
    string Title,
    decimal Amount,
    DateTime ExpenseDate,
    string Status,
    string CategoryName,
    string CategoryColor,
    string SubmittedByName,
    DateTime CreatedAt);
