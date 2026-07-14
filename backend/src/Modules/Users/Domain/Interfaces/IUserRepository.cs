using Personal.FinanceTracker.Users.Domain.Entities;

namespace Personal.FinanceTracker.Users.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken ct = default);
    Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken ct = default);
}