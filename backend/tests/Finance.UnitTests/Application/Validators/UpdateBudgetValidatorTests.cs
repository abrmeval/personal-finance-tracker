using FluentValidation.TestHelper;
using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
using Personal.FinanceTracker.Finance.Application.Validators;
using Personal.FinanceTracker.Finance.Domain.Enums;

namespace Finance.UnitTests.Application.Validators;

public class UpdateBudgetValidatorTests
{
    private readonly UpdateBudgetValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var request = new UpdateBudgetRequest(
            Guid.NewGuid(), "Monthly groceries", 500m, BudgetPeriod.Monthly);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyCategoryId_Fails()
    {
        var request = new UpdateBudgetRequest(
            Guid.Empty, "Monthly groceries", 500m, BudgetPeriod.Monthly);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyName_Fails(string? name)
    {
        var request = new UpdateBudgetRequest(
            Guid.NewGuid(), name!, 500m, BudgetPeriod.Monthly);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceeds150Characters_Fails()
    {
        var request = new UpdateBudgetRequest(
            Guid.NewGuid(), new string('a', 151), 500m, BudgetPeriod.Monthly);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_LimitAmountLessThanOrEqualToZero_Fails(decimal limitAmount)
    {
        var request = new UpdateBudgetRequest(
            Guid.NewGuid(), "Monthly groceries", limitAmount, BudgetPeriod.Monthly);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.LimitAmount);
    }

    [Fact]
    public void Validate_LimitAmountExceedsOneBillion_Fails()
    {
        var request = new UpdateBudgetRequest(
            Guid.NewGuid(), "Monthly groceries", 1_000_000_001m, BudgetPeriod.Monthly);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.LimitAmount);
    }

    [Fact]
    public void Validate_InvalidPeriod_Fails()
    {
        var request = new UpdateBudgetRequest(
            Guid.NewGuid(), "Monthly groceries", 500m, (BudgetPeriod)999);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Period);
    }
}
