using Microsoft.Extensions.Logging;
using Personal.FinanceTracker.Finance.Domain.Interfaces;
using Personal.FinanceTracker.Reporting.Application.Interfaces;
using Personal.FinanceTracker.Reporting.Domain.Entities;
using Personal.FinanceTracker.Reporting.Domain.Interfaces;
using Personal.FinanceTracker.Shared.Constants;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Reporting.Infrastructure.Services;

public sealed class ReportingService(
    ITransactionRepository transactionRepository,
    IMonthlySummaryRepository monthlySummaryRepository,
    ILogger<ReportingService> logger) : IReportingService
{
    public async Task<Result<int>> GenerateMonthlyReportsAsync(
        int year,
        int month,
        CancellationToken ct = default)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
        {
            return Result<int>.Failure(new(
                ApiErrorCode.InvalidReportParameters, "Invalid year or month."));
        }

        var totals = await transactionRepository.GetUserMonthlyTotalsAsync(year, month, ct);

        var generated = 0;
        foreach (var userTotals in totals)
        {
            var existing = await monthlySummaryRepository
                .GetByUserAndPeriodAsync(userTotals.UserId, year, month, ct);

            if (existing is null)
            {
                await monthlySummaryRepository.AddAsync(
                    MonthlySummary.Create(userTotals.UserId, year, month, userTotals.TotalIncome, userTotals.TotalExpenses),
                    ct);
            }
            else
            {
                existing.Update(userTotals.TotalIncome, userTotals.TotalExpenses);
            }

            generated++;
        }

        await monthlySummaryRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Generated {Count} monthly summaries for {Year}-{Month:00}",
            generated, year, month);

        return Result<int>.Success(generated);
    }
}
