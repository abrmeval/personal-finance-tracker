using Microsoft.EntityFrameworkCore;
using Personal.FinanceTracker.Users.Domain.Entities;
using Personal.FinanceTracker.Users.Domain.Interfaces;
using Personal.FinanceTracker.Users.Infrastructure.Data;
namespace Personal.FinanceTracker.Users.Infrastructure.Repositories;

public sealed class UserRepository(UsersDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant().Trim(), ct);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => await context.Users
            .AnyAsync(u => u.Email == email.ToLowerInvariant().Trim(), ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await context.Users.AddAsync(user, ct);

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken ct = default)
        => await context.RefreshTokens.AddAsync(refreshToken, ct);

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken ct = default)
        => await context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, ct);
            
    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}