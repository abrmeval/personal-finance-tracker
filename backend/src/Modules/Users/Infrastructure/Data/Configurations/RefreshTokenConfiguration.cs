using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Personal.FinanceTracker.Users.Domain.Entities;
namespace Personal.FinanceTracker.Users.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for the RefreshToken entity using Fluent API.
/// </summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        builder.Property(rt => rt.Token)
            .HasColumnName("token")
            .HasMaxLength(512)
            .IsRequired();
        builder.Property(rt => rt.UserId)
            .HasColumnName("user_id")
            .IsRequired();
        builder.Property(rt => rt.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamptz")
            .IsRequired();
        builder.Property(rt => rt.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")  // stores as UTC in PostgreSQL
            .HasDefaultValueSql("now()")
            .IsRequired();
        builder.Property(rt => rt.IsRevoked)
            .HasColumnName("is_revoked")
            .HasDefaultValue(false)
            .IsRequired();
        builder.Property(rt => rt.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamptz");  // stores as UTC in PostgreSQL
            
        // Ignore computed properties that are not mapped to database columns    
        builder.Ignore(rt => rt.IsExpired);
        builder.Ignore(rt => rt.IsActive);

        builder.HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("idx_refresh_tokens_token");
        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("idx_refresh_tokens_user_id");
    }
}