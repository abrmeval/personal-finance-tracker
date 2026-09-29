using Microsoft.EntityFrameworkCore;
using Personal.FinanceTracker.Reporting.Domain.Entities;
using Personal.FinanceTracker.Reporting.Domain.Interfaces;
using Personal.FinanceTracker.Reporting.Infrastructure.Data;

namespace Personal.FinanceTracker.Reporting.Infrastructure.Repositories;

public sealed class MonthlySummaryRepository(ReportingDbContext context) : IMonthlySummaryRepository
{
    public async Task<MonthlySummary?> GetByUserAndPeriodAsync(
        Guid userId,
        int year,
        int month,
        CancellationToken ct = default)
        => await context.MonthlySummaries
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Year == year && s.Month == month, ct);

    public async Task AddAsync(MonthlySummary summary, CancellationToken ct = default)
        => await context.MonthlySummaries.AddAsync(summary, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}
