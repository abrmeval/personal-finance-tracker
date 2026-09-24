using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Personal.FinanceTracker.Reporting.Domain.Entities;

namespace Personal.FinanceTracker.Reporting.Infrastructure.Data.Configurations;

public sealed class MonthlySummaryConfiguration : IEntityTypeConfiguration<MonthlySummary>
{
    public void Configure(EntityTypeBuilder<MonthlySummary> builder)
    {
        builder.ToTable("monthly_summaries");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(s => s.Year)
            .HasColumnName("year")
            .IsRequired();

        builder.Property(s => s.Month)
            .HasColumnName("month")
            .IsRequired();

        builder.Property(s => s.TotalIncome)
            .HasColumnName("total_income")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(s => s.TotalExpenses)
            .HasColumnName("total_expenses")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(s => s.NetAmount)
            .HasColumnName("net_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("idx_monthly_summaries_user_id");

        builder.HasIndex(s => new { s.UserId, s.Year, s.Month })
            .IsUnique()
            .HasDatabaseName("idx_monthly_summaries_user_period");
    }
}
