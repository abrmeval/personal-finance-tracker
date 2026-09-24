using Personal.FinanceTracker.Reporting.Domain.Entities;

namespace Personal.FinanceTracker.Reporting.Domain.Interfaces;

public interface IMonthlySummaryRepository
{
    Task<MonthlySummary?> GetByUserAndPeriodAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken ct = default);

    Task AddAsync(MonthlySummary summary, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
