using Microsoft.Extensions.Logging;
using Personal.FinanceTracker.Shared.Models;
using Personal.FinanceTracker.Users.Application.DTOs.Requests;
using Personal.FinanceTracker.Users.Application.DTOs.Responses;
using Personal.FinanceTracker.Users.Application.Interfaces;
using Personal.FinanceTracker.Users.Domain.Entities;
using Personal.FinanceTracker.Users.Domain.Interfaces;

namespace Personal.FinanceTracker.Users.Infrastructure.Services;

public sealed class UserService(
    IUserRepository repository,
    ITokenService tokenService,
    IJwtSettings jwtSettings,
    ILogger<UserService> logger) : IUserService
{
    private readonly IJwtSettings _jwtSettings = jwtSettings;

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await repository.EmailExistsAsync(request.Email, ct))
        {
            logger.LogWarning("Registration attempted with existing email {Email}", request.Email);
            return Result<AuthResponse>.Failure(new("RESOURCE_ALREADY_EXISTS", "An account with this email already exists."));
        }
        // Hash the password using BCrypt and create the user entity
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = User.Create(request.Email, passwordHash, request.FirstName, request.LastName);
        await repository.AddAsync(user, ct);

        var refreshTokenValue = tokenService.GenerateRefreshToken();
        var refreshTokenExpiry = GetRefreshTokenExpiry();
        var refreshToken = RefreshToken.Create(user.Id, refreshTokenValue, refreshTokenExpiry);
        await repository.AddRefreshTokenAsync(refreshToken, ct);
        await repository.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} registered successfully", user.Id);
        return BuildAuthResponse(user, refreshTokenValue);

    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await repository.GetByEmailAsync(request.Email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Failed login attempt for email {Email}", request.Email);
            return Result<AuthResponse>.Failure(new("INVALID_CREDENTIALS", "Invalid email or password."));
        }
        var refreshTokenValue = tokenService.GenerateRefreshToken();
        var refreshTokenExpiry = GetRefreshTokenExpiry();
        var refreshToken = RefreshToken.Create(user.Id, refreshTokenValue, refreshTokenExpiry);
        await repository.AddRefreshTokenAsync(refreshToken, ct);
        await repository.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} logged in successfully", user.Id);
        return BuildAuthResponse(user, refreshTokenValue);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var storedToken = await repository.GetRefreshTokenAsync(refreshToken, ct);

        /// Validate the token exists and is active before proceeding with generating new tokens. This prevents abuse of invalid or revoked tokens.
        if (storedToken is null || !storedToken.IsActive)
        {
            logger.LogWarning("Invalid or expired refresh token used");
            return Result<AuthResponse>.Failure(new("INVALID_TOKEN", "Invalid or expired refresh token."));
        }
        var user = await repository.GetByIdAsync(storedToken.UserId, ct);

        if (user is null)
            return Result<AuthResponse>.Failure(new("RESOURCE_NOT_FOUND", "User not found."));

        storedToken.Revoke();

        var newRefreshTokenValue = tokenService.GenerateRefreshToken();
        var newRefreshToken = RefreshToken.Create(user.Id, newRefreshTokenValue, GetRefreshTokenExpiry());
        await repository.AddRefreshTokenAsync(newRefreshToken, ct);
        await repository.SaveChangesAsync(ct);

        return BuildAuthResponse(user, newRefreshTokenValue);
    }

    public async Task<Result<bool>> RevokeTokenAsync(Guid userId, string refreshToken, CancellationToken ct = default)
    {
        // Ensure the token belongs to the user and is active before revoking
        // This prevents users from revoking tokens that aren't theirs or are already invalid
        var storedToken = await repository.GetRefreshTokenAsync(refreshToken, ct);

        if (storedToken is null || storedToken.UserId != userId || !storedToken.IsActive)
            return Result<bool>.Failure(new("INVALID_TOKEN", "The token is invalid or has already been revoked."));

        storedToken.Revoke();
        await repository.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    private Result<AuthResponse> BuildAuthResponse(User user, string refreshTokenValue)
    {
        string accessToken = tokenService.GenerateAccessToken(user);
        int expiryMinutes = _jwtSettings.ExpiryMinutes;


        return Result<AuthResponse>.Success(new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            ExpiresIn: expiryMinutes * 60,
            User: new UserResponse(user.Id, user.Email, user.FirstName, user.LastName)));
    }

    private DateTime GetRefreshTokenExpiry()
    {
        return DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);
    }
}