using Microsoft.EntityFrameworkCore;
using Personal.FinanceTracker.Users.Domain.Entities;
namespace Personal.FinanceTracker.Users.Infrastructure.Data;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Configure the model and relationships using Fluent API.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure the entity models.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("users");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
    }
}