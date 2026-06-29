namespace Personal.FinanceTracker.Finance.Application.DTOs.Responses;

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string? Icon,
    string? Color,
    DateTime CreatedAt,
    DateTime? UpdatedAt);