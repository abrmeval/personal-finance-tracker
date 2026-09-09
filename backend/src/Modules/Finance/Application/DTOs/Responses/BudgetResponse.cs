using Personal.FinanceTracker.Finance.Domain.Enums;

namespace Personal.FinanceTracker.Finance.Application.DTOs.Responses;

public sealed record BudgetResponse(
    Guid Id,
    Guid CategoryId,
    string Name,
    decimal LimitAmount,
    BudgetPeriod Period,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
