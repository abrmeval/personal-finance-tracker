using Personal.FinanceTracker.Shared.Models;
using Personal.FinanceTracker.Users.Application.DTOs.Requests;
using Personal.FinanceTracker.Users.Application.DTOs.Responses;
namespace Personal.FinanceTracker.Users.Application.Interfaces;

public interface IUserService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<Result<bool>> RevokeTokenAsync(Guid userId, string refreshToken, CancellationToken ct = default);
}