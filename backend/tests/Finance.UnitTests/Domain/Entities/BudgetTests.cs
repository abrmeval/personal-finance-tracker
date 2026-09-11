using Personal.FinanceTracker.Finance.Domain.Entities;
using Personal.FinanceTracker.Finance.Domain.Enums;

namespace Finance.UnitTests.Domain.Entities;

public class BudgetTests
{
    private static readonly Guid TestUserId = Guid.NewGuid();
    private static readonly Guid TestCategoryId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_ReturnsBudget()
    {
        var budget = Budget.Create(
            TestUserId, TestCategoryId, "Monthly groceries", 500m, BudgetPeriod.Monthly);

        Assert.NotEqual(Guid.Empty, budget.Id);
        Assert.Equal(TestUserId, budget.UserId);
        Assert.Equal(TestCategoryId, budget.CategoryId);
        Assert.Equal("Monthly groceries", budget.Name);
        Assert.Equal(500m, budget.LimitAmount);
        Assert.Equal(BudgetPeriod.Monthly, budget.Period);
        Assert.True(budget.IsActive);
        Assert.NotEqual(default, budget.CreatedAt);
    }

    [Fact]
    public void Create_EmptyCategoryId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Budget.Create(TestUserId, Guid.Empty, "Groceries", 500m, BudgetPeriod.Monthly));
        Assert.Contains("Category ID", ex.Message);
    }

    [Fact]
    public void Update_ValidInput_UpdatesAllFields()
    {
        var budget = Budget.Create(
            TestUserId, TestCategoryId, "Monthly groceries", 500m, BudgetPeriod.Monthly);
        var newCategoryId = Guid.NewGuid();

        budget.Update(newCategoryId, "Weekly dining out", 200m, BudgetPeriod.Weekly);

        Assert.Equal(newCategoryId, budget.CategoryId);
        Assert.Equal("Weekly dining out", budget.Name);
        Assert.Equal(200m, budget.LimitAmount);
        Assert.Equal(BudgetPeriod.Weekly, budget.Period);
        Assert.NotEqual(default, budget.UpdatedAt);
    }

    [Fact]
    public void Update_SameCategoryId_KeepsCategory()
    {
        var budget = Budget.Create(
            TestUserId, TestCategoryId, "Groceries", 500m, BudgetPeriod.Monthly);

        budget.Update(TestCategoryId, "Groceries", 600m, BudgetPeriod.Monthly);

        Assert.Equal(TestCategoryId, budget.CategoryId);
    }

    [Fact]
    public void Update_EmptyCategoryId_ThrowsArgumentException()
    {
        var budget = Budget.Create(
            TestUserId, TestCategoryId, "Groceries", 500m, BudgetPeriod.Monthly);

        var ex = Assert.Throws<ArgumentException>(() =>
            budget.Update(Guid.Empty, "Groceries", 500m, BudgetPeriod.Monthly));
        Assert.Contains("Category ID", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_EmptyName_ThrowsArgumentException(string? name)
    {
        var budget = Budget.Create(
            TestUserId, TestCategoryId, "Groceries", 500m, BudgetPeriod.Monthly);

        var ex = Assert.Throws<ArgumentException>(() =>
            budget.Update(TestCategoryId, name!, 500m, BudgetPeriod.Monthly));
        Assert.Contains("name", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Update_LimitAmountLessThanOrEqualToZero_ThrowsArgumentException(decimal limitAmount)
    {
        var budget = Budget.Create(
            TestUserId, TestCategoryId, "Groceries", 500m, BudgetPeriod.Monthly);

        var ex = Assert.Throws<ArgumentException>(() =>
            budget.Update(TestCategoryId, "Groceries", limitAmount, BudgetPeriod.Monthly));
        Assert.Contains("greater than zero", ex.Message);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var budget = Budget.Create(
            TestUserId, TestCategoryId, "Groceries", 500m, BudgetPeriod.Monthly);

        budget.Deactivate();

        Assert.False(budget.IsActive);
        Assert.NotEqual(default, budget.UpdatedAt);
    }
}
