using Personal.FinanceTracker.Users.Domain.Entities;

namespace Users.UnitTests.Domain.Entities;

public class UserTests
{
    [Fact]
    public void Create_ValidInput_ReturnsUser()
    {
        var user = User.Create("test@example.com", "hashedpassword", "John", "Doe");

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("hashedpassword", user.PasswordHash);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.True(user.IsActive);
        Assert.NotEqual(default, user.CreatedAt);
    }

    [Fact]
    public void Create_TrimsEmailAndNames()
    {
        var user = User.Create("  Test@Example.COM  ", "hash", "  John  ", "  Doe  ");

        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyEmail_ThrowsArgumentException(string? email)
    {
        var ex = Assert.Throws<ArgumentException>(() => User.Create(email!, "hash", "John", "Doe"));
        Assert.Contains("Email", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyPasswordHash_ThrowsArgumentException(string? passwordHash)
    {
        var ex = Assert.Throws<ArgumentException>(() => User.Create("test@example.com", passwordHash!, "John", "Doe"));
        Assert.Contains("Password hash", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyFirstName_ThrowsArgumentException(string? firstName)
    {
        var ex = Assert.Throws<ArgumentException>(() => User.Create("test@example.com", "hash", firstName!, "Doe"));
        Assert.Contains("First name", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyLastName_ThrowsArgumentException(string? lastName)
    {
        var ex = Assert.Throws<ArgumentException>(() => User.Create("test@example.com", "hash", "John", lastName!));
        Assert.Contains("Last name", ex.Message);
    }

    [Fact]
    public void UpdatePassword_ValidHash_UpdatesPassword()
    {
        var user = User.Create("test@example.com", "oldhash", "John", "Doe");

        user.UpdatePassword("newhash");

        Assert.Equal("newhash", user.PasswordHash);
        Assert.NotNull(user.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdatePassword_EmptyHash_ThrowsArgumentException(string? newHash)
    {
        var user = User.Create("test@example.com", "hash", "John", "Doe");
        var ex = Assert.Throws<ArgumentException>(() => user.UpdatePassword(newHash!));
        Assert.Contains("Password hash", ex.Message);
    }
}
