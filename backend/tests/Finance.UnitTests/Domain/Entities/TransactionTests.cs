using Personal.FinanceTracker.Finance.Domain.Entities;
using Personal.FinanceTracker.Finance.Domain.Enums;

namespace Finance.UnitTests.Domain.Entities;

public class TransactionTests
{
    private static readonly Guid TestUserId = Guid.NewGuid();
    private static readonly Guid TestCategoryId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_ReturnsTransaction()
    {
        var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var transaction = Transaction.Create(
            TestUserId, "Grocery shopping", 150.50m,
            TransactionType.Expense, date, TestCategoryId, "Weekly groceries");

        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.Equal(TestUserId, transaction.UserId);
        Assert.Equal(TestCategoryId, transaction.CategoryId);
        Assert.Equal("Grocery shopping", transaction.Description);
        Assert.Equal(150.50m, transaction.Amount);
        Assert.Equal(TransactionType.Expense, transaction.Type);
        Assert.Equal(date, transaction.Date);
        Assert.Equal("Weekly groceries", transaction.Notes);
        Assert.True(transaction.IsActive);
        Assert.NotEqual(default, transaction.CreatedAt);
    }

    [Fact]
    public void Create_WithoutOptionalFields_SetsNulls()
    {
        var date = DateTime.UtcNow;
        var transaction = Transaction.Create(
            TestUserId, "Salary", 5000m, TransactionType.Income, date);

        Assert.Null(transaction.CategoryId);
        Assert.Null(transaction.Notes);
    }

    [Fact]
    public void Create_EmptyUserId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Transaction.Create(Guid.Empty, "Desc", 100m, TransactionType.Expense, DateTime.UtcNow));
        Assert.Contains("User ID", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyDescription_ThrowsArgumentException(string? description)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Transaction.Create(TestUserId, description!, 100m, TransactionType.Expense, DateTime.UtcNow));
        Assert.Contains("Description", ex.Message);
    }

    [Fact]
    public void Create_DescriptionExceeds500Characters_ThrowsArgumentException()
    {
        var longDesc = new string('a', 501);
        var ex = Assert.Throws<ArgumentException>(() =>
            Transaction.Create(TestUserId, longDesc, 100m, TransactionType.Expense, DateTime.UtcNow));
        Assert.Contains("500 characters", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Create_AmountLessThanOrEqualToZero_ThrowsArgumentException(decimal amount)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Transaction.Create(TestUserId, "Desc", amount, TransactionType.Expense, DateTime.UtcNow));
        Assert.Contains("greater than zero", ex.Message);
    }

    [Fact]
    public void Create_DefaultDate_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Transaction.Create(TestUserId, "Desc", 100m, TransactionType.Expense, default));
        Assert.Contains("valid date", ex.Message);
    }

    [Fact]
    public void Create_TrimsDescriptionAndNotes()
    {
        var date = DateTime.UtcNow;
        var transaction = Transaction.Create(
            TestUserId, "  Description  ", 100m,
            TransactionType.Expense, date, null, "  Notes  ");

        Assert.Equal("Description", transaction.Description);
        Assert.Equal("Notes", transaction.Notes);
    }

    [Fact]
    public void Create_EmptyNotes_SetsNull()
    {
        var date = DateTime.UtcNow;
        var transaction = Transaction.Create(
            TestUserId, "Desc", 100m, TransactionType.Expense, date, null, "   ");

        Assert.Null(transaction.Notes);
    }

    [Fact]
    public void Create_UnspecifiedDate_ConvertsToUtc()
    {
        var localDate = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var transaction = Transaction.Create(
            TestUserId, "Desc", 100m, TransactionType.Expense, localDate);

        Assert.Equal(DateTimeKind.Utc, transaction.Date.Kind);
    }

    [Fact]
    public void Create_LocalDate_ConvertsToUtc()
    {
        var localDate = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Local);
        var transaction = Transaction.Create(
            TestUserId, "Desc", 100m, TransactionType.Expense, localDate);

        Assert.Equal(DateTimeKind.Utc, transaction.Date.Kind);
    }

    [Fact]
    public void Update_ValidInput_UpdatesProperties()
    {
        var date = DateTime.UtcNow;
        var transaction = Transaction.Create(
            TestUserId, "Old Desc", 50m, TransactionType.Expense, date);

        var newDate = new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        transaction.Update("New Desc", 200m, TransactionType.Income, newDate, TestCategoryId, "Updated notes");

        Assert.Equal("New Desc", transaction.Description);
        Assert.Equal(200m, transaction.Amount);
        Assert.Equal(TransactionType.Income, transaction.Type);
        Assert.Equal(newDate, transaction.Date);
        Assert.Equal(TestCategoryId, transaction.CategoryId);
        Assert.Equal("Updated notes", transaction.Notes);
        Assert.NotNull(transaction.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_EmptyDescription_ThrowsArgumentException(string? description)
    {
        var transaction = Transaction.Create(
            TestUserId, "Desc", 100m, TransactionType.Expense, DateTime.UtcNow);

        var ex = Assert.Throws<ArgumentException>(() =>
            transaction.Update(description!, 100m, TransactionType.Expense, DateTime.UtcNow));
        Assert.Contains("Description", ex.Message);
    }

    [Fact]
    public void Update_AmountLessThanOrEqualToZero_ThrowsArgumentException()
    {
        var transaction = Transaction.Create(
            TestUserId, "Desc", 100m, TransactionType.Expense, DateTime.UtcNow);

        var ex = Assert.Throws<ArgumentException>(() =>
            transaction.Update("Desc", 0m, TransactionType.Expense, DateTime.UtcNow));
        Assert.Contains("greater than zero", ex.Message);
    }

    [Fact]
    public void Update_DefaultDate_ThrowsArgumentException()
    {
        var transaction = Transaction.Create(
            TestUserId, "Desc", 100m, TransactionType.Expense, DateTime.UtcNow);

        var ex = Assert.Throws<ArgumentException>(() =>
            transaction.Update("Desc", 100m, TransactionType.Expense, default));
        Assert.Contains("valid date", ex.Message);
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var transaction = Transaction.Create(
            TestUserId, "Desc", 100m, TransactionType.Expense, DateTime.UtcNow);

        transaction.Deactivate();

        Assert.False(transaction.IsActive);
        Assert.NotNull(transaction.UpdatedAt);
    }
}
