using Personal.FinanceTracker.Reporting.Application.DTOs.Responses;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Reporting.Application.Interfaces;

public interface IDashboardService
{
    Task<Result<DashboardSummaryResponse>> GetSummaryAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<MonthlyTotalsResponse>>> GetIncomeVsExpensesAsync(
        Guid userId,
        int? months,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<CategoryBreakdownResponse>>> GetCategoryBreakdownAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default);
}
