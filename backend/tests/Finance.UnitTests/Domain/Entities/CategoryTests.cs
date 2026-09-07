using Personal.FinanceTracker.Finance.Domain.Entities;

namespace Finance.UnitTests.Domain.Entities;

public class CategoryTests
{
    private static readonly Guid TestUserId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_ReturnsCategory()
    {
        var category = Category.Create(TestUserId, "Groceries", "🛒", "#FF5733");

        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.Equal(TestUserId, category.UserId);
        Assert.Equal("Groceries", category.Name);
        Assert.Equal("🛒", category.Icon);
        Assert.Equal("#FF5733", category.Color);
        Assert.True(category.IsActive);
        Assert.NotEqual(default, category.CreatedAt);
    }

    [Fact]
    public void Create_WithoutOptionalFields_SetsNulls()
    {
        var category = Category.Create(TestUserId, "Utilities");

        Assert.Equal("Utilities", category.Name);
        Assert.Null(category.Icon);
        Assert.Null(category.Color);
    }

    [Fact]
    public void Create_EmptyUserId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => Category.Create(Guid.Empty, "Name"));
        Assert.Contains("User ID", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_ThrowsArgumentException(string? name)
    {
        var ex = Assert.Throws<ArgumentException>(() => Category.Create(TestUserId, name!));
        Assert.Contains("name", ex.Message);
    }

    [Fact]
    public void Create_NameExceeds100Characters_ThrowsArgumentException()
    {
        var longName = new string('a', 101);
        var ex = Assert.Throws<ArgumentException>(() => Category.Create(TestUserId, longName));
        Assert.Contains("100 characters", ex.Message);
    }

    [Fact]
    public void Create_TrimsNameAndOptionalFields()
    {
        var category = Category.Create(TestUserId, "  Groceries  ", "  🛒  ", "  #FF5733  ");

        Assert.Equal("Groceries", category.Name);
        Assert.Equal("🛒", category.Icon);
        Assert.Equal("#FF5733", category.Color);
    }

    [Fact]
    public void Create_EmptyOptionalFields_SetsNulls()
    {
        var category = Category.Create(TestUserId, "Name", "   ", "   ");

        Assert.Null(category.Icon);
        Assert.Null(category.Color);
    }

    [Fact]
    public void Update_ValidInput_UpdatesProperties()
    {
        var category = Category.Create(TestUserId, "Old Name", "old-icon", "#000000");
        var originalUpdatedAt = category.UpdatedAt;

        category.Update("New Name", "new-icon", "#FFFFFF");

        Assert.Equal("New Name", category.Name);
        Assert.Equal("new-icon", category.Icon);
        Assert.Equal("#FFFFFF", category.Color);
        Assert.NotNull(category.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_EmptyName_ThrowsArgumentException(string? name)
    {
        var category = Category.Create(TestUserId, "Name");
        var ex = Assert.Throws<ArgumentException>(() => category.Update(name!));
        Assert.Contains("name", ex.Message);
    }

    [Fact]
    public void Update_NameExceeds100Characters_ThrowsArgumentException()
    {
        var category = Category.Create(TestUserId, "Name");
        var longName = new string('a', 101);
        var ex = Assert.Throws<ArgumentException>(() => category.Update(longName));
        Assert.Contains("100 characters", ex.Message);
    }

    [Fact]
    public void Update_TrimsNameAndOptionalFields()
    {
        var category = Category.Create(TestUserId, "Name");

        category.Update("  Updated  ", "  icon  ", "  #ABC  ");

        Assert.Equal("Updated", category.Name);
        Assert.Equal("icon", category.Icon);
        Assert.Equal("#ABC", category.Color);
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var category = Category.Create(TestUserId, "Name");

        category.Deactivate();

        Assert.False(category.IsActive);
        Assert.NotNull(category.UpdatedAt);
    }
}
