using Personal.FinanceTracker.Finance.Domain.Enums;

namespace Personal.FinanceTracker.Finance.Application.DTOs.Responses;

public sealed record BudgetWithSpendingResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    decimal LimitAmount,
    BudgetPeriod Period,
    decimal SpentAmount,
    decimal RemainingAmount,
    decimal PercentageUsed,
    bool IsOverBudget,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
