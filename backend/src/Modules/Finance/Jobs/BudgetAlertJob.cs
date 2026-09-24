using Microsoft.Extensions.Logging;
using Personal.FinanceTracker.Finance.Application.Interfaces;
using TickerQ.Utilities.Base;

namespace Personal.FinanceTracker.Finance.Jobs;

public sealed class BudgetAlertJob(
    IBudgetService budgetService,
    ILogger<BudgetAlertJob> logger)
{
    private const decimal AlertThresholdPercentage = 80m;

    [TickerFunction("check-budget-alerts", cronExpression: "0 */6 * * *")]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("BudgetAlertJob starting with threshold {Threshold}%", AlertThresholdPercentage);

        var result = await budgetService.GetBudgetsNearLimitAsync(AlertThresholdPercentage, ct);
        if (result.IsFailure)
        {
            logger.LogError("BudgetAlertJob failed: {Error}", result.Error?.Description);
            return;
        }

        var budgets = result.Value ?? [];
        foreach (var budget in budgets)
        {
            logger.LogWarning(
                "Budget alert: user {UserId}, budget \"{BudgetName}\" ({CategoryName}) at {Percentage}% of {Period} limit — spent {Spent} of {Limit}",
                budget.UserId,
                budget.Name,
                budget.CategoryName,
                budget.PercentageUsed,
                budget.Period,
                budget.SpentAmount,
                budget.LimitAmount);
        }

        logger.LogInformation("BudgetAlertJob completed: {Count} budgets at or above threshold", budgets.Count);
    }
}
