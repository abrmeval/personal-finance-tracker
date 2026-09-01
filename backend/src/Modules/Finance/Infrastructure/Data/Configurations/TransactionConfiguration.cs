using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Personal.FinanceTracker.Finance.Domain.Entities;
using Personal.FinanceTracker.Finance.Domain.Enums;

namespace Personal.FinanceTracker.Finance.Infrastructure.Data.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(t => t.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(t => t.CategoryId)
            .HasColumnName("category_id");

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(t => t.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(t => t.Date)
            .HasColumnName("date")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(t => t.Notes)
            .HasColumnName("notes")
            .HasMaxLength(2000);

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        builder.Property(t => t.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("idx_transactions_user_id");

        builder.HasIndex(t => t.Date)
            .HasDatabaseName("idx_transactions_date");

        builder.HasIndex(t => new { t.UserId, t.CategoryId })
            .HasDatabaseName("idx_transactions_user_category");
    }
}