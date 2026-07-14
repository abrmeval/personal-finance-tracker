using Personal.FinanceTracker.Finance.Domain.Enums;

namespace Personal.FinanceTracker.Finance.Application.DTOs.Responses;

public sealed record TransactionResponse(
    Guid Id,
    string Description,
    decimal Amount,
    TransactionType Type,
    DateTime Date,
    Guid? CategoryId,
    string? CategoryName,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt);