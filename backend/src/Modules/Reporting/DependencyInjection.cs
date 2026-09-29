using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Personal.FinanceTracker.Reporting.Application.Interfaces;
using Personal.FinanceTracker.Reporting.Domain.Interfaces;
using Personal.FinanceTracker.Reporting.Infrastructure.Data;
using Personal.FinanceTracker.Reporting.Infrastructure.Repositories;
using Personal.FinanceTracker.Reporting.Infrastructure.Services;

namespace Personal.FinanceTracker.Reporting;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ReportingDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "reports");
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                    npgsqlOptions.CommandTimeout(30);
                }));

        services.AddScoped<IMonthlySummaryRepository, MonthlySummaryRepository>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportingService, ReportingService>();

        return services;
    }
}
