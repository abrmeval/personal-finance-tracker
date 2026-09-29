using Microsoft.EntityFrameworkCore;
using Personal.FinanceTracker.Reporting.Domain.Entities;

namespace Personal.FinanceTracker.Reporting.Infrastructure.Data;

public sealed class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : DbContext(options)
{
    public DbSet<MonthlySummary> MonthlySummaries => Set<MonthlySummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("reports");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReportingDbContext).Assembly);
    }
}
