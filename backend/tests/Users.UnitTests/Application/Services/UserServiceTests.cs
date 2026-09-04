using Microsoft.Extensions.Logging;
using NSubstitute;
using Personal.FinanceTracker.Shared.Constants;
using Personal.FinanceTracker.Users.Application.DTOs.Requests;
using Personal.FinanceTracker.Users.Application.Interfaces;
using Personal.FinanceTracker.Users.Domain.Entities;
using Personal.FinanceTracker.Users.Domain.Interfaces;
using Personal.FinanceTracker.Users.Infrastructure.Services;

namespace Users.UnitTests.Application.Services;

public class UserServiceTests
{
    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IJwtSettings _jwtSettings = Substitute.For<IJwtSettings>();
    private readonly ILogger<UserService> _logger = Substitute.For<ILogger<UserService>>();
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _jwtSettings.ExpiryMinutes.Returns(15);
        _jwtSettings.RefreshTokenExpiryDays.Returns(7);
        _tokenService.GenerateAccessToken(Arg.Any<User>()).Returns("fake-access-token");
        _tokenService.GenerateRefreshToken().Returns("fake-refresh-token");
        _userService = new UserService(_repository, _tokenService, _jwtSettings, _logger);
    }

    #region RegisterAsync

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ReturnsFailure()
    {
        var request = new RegisterRequest("test@example.com", "Password123!", "John", "Doe");
        _repository.EmailExistsAsync(request.Email, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _userService.RegisterAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.ResourceAlreadyExists, result.Error!.Code);
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesUserAndReturnsAuthResponse()
    {
        var request = new RegisterRequest("test@example.com", "Password123!", "John", "Doe");
        _repository.EmailExistsAsync(request.Email, Arg.Any<CancellationToken>()).Returns(false);
        User? capturedUser = null;
        await _repository.AddAsync(Arg.Do<User>(u => capturedUser = u), Arg.Any<CancellationToken>());

        var result = await _userService.RegisterAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal("fake-access-token", result.Value.AccessToken);
        Assert.Equal("fake-refresh-token", result.Value.RefreshToken);
        Assert.Equal(15 * 60, result.Value.ExpiresIn);
        Assert.Equal("test@example.com", result.Value.User.Email);
        Assert.Equal("John", result.Value.User.FirstName);
        Assert.Equal("Doe", result.Value.User.LastName);

        Assert.NotNull(capturedUser);
        Assert.Equal("test@example.com", capturedUser.Email);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", capturedUser.PasswordHash));

        await _repository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).AddRefreshTokenAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region LoginAsync

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsFailure()
    {
        var request = new LoginRequest("test@example.com", "Password123!");
        _repository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _userService.LoginAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.InvalidCredentials, result.Error!.Code);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFailure()
    {
        var request = new LoginRequest("test@example.com", "WrongPassword");
        var user = User.Create("test@example.com", BCrypt.Net.BCrypt.HashPassword("Password123!"), "John", "Doe");
        _repository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _userService.LoginAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.InvalidCredentials, result.Error!.Code);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        var request = new LoginRequest("test@example.com", "Password123!");
        var user = User.Create("test@example.com", BCrypt.Net.BCrypt.HashPassword("Password123!"), "John", "Doe");
        _repository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _userService.LoginAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal("fake-access-token", result.Value.AccessToken);
        Assert.Equal("fake-refresh-token", result.Value.RefreshToken);
        Assert.Equal("test@example.com", result.Value.User.Email);

        await _repository.Received(1).AddRefreshTokenAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region RefreshTokenAsync

    [Fact]
    public async Task RefreshTokenAsync_InvalidToken_ReturnsFailure()
    {
        _repository.GetRefreshTokenAsync("invalid-token", Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var result = await _userService.RefreshTokenAsync("invalid-token");

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.InvalidToken, result.Error!.Code);
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokedToken_ReturnsFailure()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "revoked-token", DateTime.UtcNow.AddDays(7));
        token.Revoke();
        _repository.GetRefreshTokenAsync("revoked-token", Arg.Any<CancellationToken>()).Returns(token);

        var result = await _userService.RefreshTokenAsync("revoked-token");

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.InvalidToken, result.Error!.Code);
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ReturnsFailure()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "expired-token", DateTime.UtcNow.AddDays(-1));
        _repository.GetRefreshTokenAsync("expired-token", Arg.Any<CancellationToken>()).Returns(token);

        var result = await _userService.RefreshTokenAsync("expired-token");

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.InvalidToken, result.Error!.Code);
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewAuthResponse()
    {
        var userId = Guid.NewGuid();
        var token = RefreshToken.Create(userId, "valid-token", DateTime.UtcNow.AddDays(7));
        var user = User.Create("test@example.com", "hash", "John", "Doe");
        _repository.GetRefreshTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(token);
        _repository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _userService.RefreshTokenAsync("valid-token");

        Assert.True(result.IsSuccess);
        Assert.Equal("fake-access-token", result.Value.AccessToken);
        Assert.Equal("fake-refresh-token", result.Value.RefreshToken);
        Assert.True(token.IsRevoked);

        await _repository.Received(1).AddRefreshTokenAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshTokenAsync_UserNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var token = RefreshToken.Create(userId, "valid-token", DateTime.UtcNow.AddDays(7));
        _repository.GetRefreshTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(token);
        _repository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _userService.RefreshTokenAsync("valid-token");

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.ResourceNotFound, result.Error!.Code);
    }

    #endregion

    #region RevokeTokenAsync

    [Fact]
    public async Task RevokeTokenAsync_TokenNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _repository.GetRefreshTokenAsync("token", Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var result = await _userService.RevokeTokenAsync(userId, "token");

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.InvalidToken, result.Error!.Code);
    }

    [Fact]
    public async Task RevokeTokenAsync_TokenBelongsToDifferentUser_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var token = RefreshToken.Create(otherUserId, "token", DateTime.UtcNow.AddDays(7));
        _repository.GetRefreshTokenAsync("token", Arg.Any<CancellationToken>()).Returns(token);

        var result = await _userService.RevokeTokenAsync(userId, "token");

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.InvalidToken, result.Error!.Code);
    }

    [Fact]
    public async Task RevokeTokenAsync_AlreadyRevokedToken_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var token = RefreshToken.Create(userId, "token", DateTime.UtcNow.AddDays(7));
        token.Revoke();
        _repository.GetRefreshTokenAsync("token", Arg.Any<CancellationToken>()).Returns(token);

        var result = await _userService.RevokeTokenAsync(userId, "token");

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.InvalidToken, result.Error!.Code);
    }

    [Fact]
    public async Task RevokeTokenAsync_ValidToken_RevokesAndReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var token = RefreshToken.Create(userId, "token", DateTime.UtcNow.AddDays(7));
        _repository.GetRefreshTokenAsync("token", Arg.Any<CancellationToken>()).Returns(token);

        var result = await _userService.RevokeTokenAsync(userId, "token");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        Assert.True(token.IsRevoked);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}
