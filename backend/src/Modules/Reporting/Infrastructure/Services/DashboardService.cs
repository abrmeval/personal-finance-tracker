using Microsoft.Extensions.Logging;
using Personal.FinanceTracker.Finance.Domain.Interfaces;
using Personal.FinanceTracker.Finance.Domain.Models;
using Personal.FinanceTracker.Reporting.Application.DTOs.Responses;
using Personal.FinanceTracker.Reporting.Application.Interfaces;
using Personal.FinanceTracker.Shared.Constants;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Reporting.Infrastructure.Services;

public sealed class DashboardService(
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    ILogger<DashboardService> logger) : IDashboardService
{
    private const int DefaultMonths = 6;
    private const int MaxMonths = 24;

    public async Task<Result<DashboardSummaryResponse>> GetSummaryAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

        var monthTotals = await transactionRepository.GetTotalsByTypeAsync(userId, monthStart, monthEnd, ct);
        var allTimeTotals = await transactionRepository.GetTotalsByTypeAsync(userId, null, null, ct);

        logger.LogDebug("Dashboard summary computed for user {UserId}", userId);

        return Result<DashboardSummaryResponse>.Success(new DashboardSummaryResponse(
            TotalBalance: allTimeTotals.TotalIncome - allTimeTotals.TotalExpenses,
            MonthlyIncome: monthTotals.TotalIncome,
            MonthlyExpenses: monthTotals.TotalExpenses));
    }

    public async Task<Result<IReadOnlyList<MonthlyTotalsResponse>>> GetIncomeVsExpensesAsync(
        Guid userId,
        int? months,
        CancellationToken ct = default)
    {
        var monthCount = months ?? DefaultMonths;
        if (monthCount is < 1 or > MaxMonths)
        {
            return Result<IReadOnlyList<MonthlyTotalsResponse>>.Failure(new(
                ApiErrorCode.InvalidReportParameters,
                $"Months must be between 1 and {MaxMonths}."));
        }

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var from = currentMonthStart.AddMonths(-(monthCount - 1));
        var to = currentMonthStart.AddMonths(1).AddTicks(-1);

        var totals = await transactionRepository.GetMonthlyTotalsAsync(userId, from, to, ct);
        var byPeriod = totals.ToDictionary(t => (t.Year, t.Month));

        var results = new List<MonthlyTotalsResponse>(monthCount);
        for (var i = monthCount - 1; i >= 0; i--)
        {
            var monthStart = currentMonthStart.AddMonths(-i);
            byPeriod.TryGetValue((monthStart.Year, monthStart.Month), out var monthTotals);
            results.Add(new MonthlyTotalsResponse(
                monthStart.Year,
                monthStart.Month,
                monthTotals?.TotalIncome ?? 0m,
                monthTotals?.TotalExpenses ?? 0m));
        }

        return Result<IReadOnlyList<MonthlyTotalsResponse>>.Success(results);
    }

    public async Task<Result<IReadOnlyList<CategoryBreakdownResponse>>> GetCategoryBreakdownAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default)
    {
        if (startDate is null || endDate is null || startDate.Value.Date > endDate.Value.Date)
        {
            return Result<IReadOnlyList<CategoryBreakdownResponse>>.Failure(new(
                ApiErrorCode.InvalidReportParameters,
                "A valid start and end date range is required."));
        }

        var from = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(endDate.Value.Date, DateTimeKind.Utc).AddDays(1).AddTicks(-1);

        var totals = await transactionRepository.GetExpenseTotalsByCategoryAsync(userId, from, to, ct);

        var categoryNames = (await categoryRepository.GetAllByUserAsync(userId, ct))
            .ToDictionary(c => c.Id, c => c.Name);

        var results = totals.Select(t => new CategoryBreakdownResponse(
            t.CategoryId,
            categoryNames.GetValueOrDefault(t.CategoryId, "Unknown"),
            t.Total)).ToList();

        return Result<IReadOnlyList<CategoryBreakdownResponse>>.Success(results);
    }
}
