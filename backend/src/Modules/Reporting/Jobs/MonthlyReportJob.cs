using Microsoft.Extensions.Logging;
using Personal.FinanceTracker.Reporting.Application.Interfaces;
using TickerQ.Utilities.Base;

namespace Personal.FinanceTracker.Reporting.Jobs;

public sealed class MonthlyReportJob(
    IReportingService reportingService,
    ILogger<MonthlyReportJob> logger)
{
    [TickerFunction("generate-monthly-reports", cronExpression: "0 0 1 * *")]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var previousMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1);

        logger.LogInformation(
            "MonthlyReportJob starting for {Year}-{Month:00}",
            previousMonth.Year, previousMonth.Month);

        var result = await reportingService.GenerateMonthlyReportsAsync(
            previousMonth.Year, previousMonth.Month, ct);

        if (result.IsFailure)
        {
            logger.LogError(
                "MonthlyReportJob failed for {Year}-{Month:00}: {Error}",
                previousMonth.Year, previousMonth.Month, result.Error?.Description);
            return;
        }

        logger.LogInformation(
            "MonthlyReportJob completed: {Count} summaries persisted",
            result.Value);
    }
}
