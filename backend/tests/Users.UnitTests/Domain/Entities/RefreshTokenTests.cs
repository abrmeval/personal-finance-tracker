using Personal.FinanceTracker.Users.Domain.Entities;

namespace Users.UnitTests.Domain.Entities;

public class RefreshTokenTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_ReturnsRefreshToken()
    {
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var token = RefreshToken.Create(UserId, "token-value", expiresAt);

        Assert.NotEqual(Guid.Empty, token.Id);
        Assert.Equal(UserId, token.UserId);
        Assert.Equal("token-value", token.Token);
        Assert.Equal(expiresAt, token.ExpiresAt);
        Assert.False(token.IsRevoked);
        Assert.NotEqual(default, token.CreatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyToken_ThrowsArgumentException(string? tokenValue)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create(UserId, tokenValue!, DateTime.UtcNow.AddDays(7)));
        Assert.Contains("Token", ex.Message);
    }

    [Fact]
    public void Revoke_SetsIsRevokedAndRevokedAt()
    {
        var token = RefreshToken.Create(UserId, "token", DateTime.UtcNow.AddDays(7));

        token.Revoke();

        Assert.True(token.IsRevoked);
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public void IsExpired_ExpiredToken_ReturnsTrue()
    {
        var expiredDate = DateTime.UtcNow.AddDays(-1);
        var token = RefreshToken.Create(UserId, "token", expiredDate);

        Assert.True(token.IsExpired);
        Assert.False(token.IsActive);
    }

    [Fact]
    public void IsActive_NonExpiredNonRevokedToken_ReturnsTrue()
    {
        var futureDate = DateTime.UtcNow.AddDays(7);
        var token = RefreshToken.Create(UserId, "token", futureDate);

        Assert.True(token.IsActive);
        Assert.False(token.IsExpired);
    }

    [Fact]
    public void IsActive_RevokedToken_ReturnsFalse()
    {
        var futureDate = DateTime.UtcNow.AddDays(7);
        var token = RefreshToken.Create(UserId, "token", futureDate);
        token.Revoke();

        Assert.False(token.IsActive);
    }
}
