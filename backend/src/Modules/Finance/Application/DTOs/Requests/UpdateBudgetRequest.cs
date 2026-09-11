using Personal.FinanceTracker.Finance.Domain.Enums;

namespace Personal.FinanceTracker.Finance.Application.DTOs.Requests;

public sealed record UpdateBudgetRequest(
    Guid CategoryId,
    string Name,
    decimal LimitAmount,
    BudgetPeriod Period);
