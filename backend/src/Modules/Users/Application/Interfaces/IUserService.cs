using Personal.FinanceTracker.Users.Application.DTOs.Requests;
using Personal.FinanceTracker.Users.Application.DTOs.Responses;
namespace Personal.FinanceTracker.Users.Application.Interfaces;

public interface IUserService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<bool> RevokeTokenAsync(Guid userId, string refreshToken, CancellationToken ct = default);
}