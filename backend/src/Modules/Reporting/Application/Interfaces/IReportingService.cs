using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Reporting.Application.Interfaces;

public interface IReportingService
{
    Task<Result<int>> GenerateMonthlyReportsAsync(
        int year,
        int month,
        CancellationToken ct = default);
}
