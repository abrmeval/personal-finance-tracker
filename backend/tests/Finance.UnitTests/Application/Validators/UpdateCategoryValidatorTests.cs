using FluentValidation.TestHelper;
using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
using Personal.FinanceTracker.Finance.Application.Validators;

namespace Finance.UnitTests.Application.Validators;

public class UpdateCategoryValidatorTests
{
    private readonly UpdateCategoryValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var request = new UpdateCategoryRequest("Updated Groceries", "🛒", "#00FF00");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var request = new UpdateCategoryRequest("", null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceeds100Characters_Fails()
    {
        var request = new UpdateCategoryRequest(new string('a', 101), null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("Name@Invalid")]
    [InlineData("Name<Invalid>")]
    public void Validate_NameWithInvalidCharacters_Fails(string name)
    {
        var request = new UpdateCategoryRequest(name, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_IconExceeds50Characters_Fails()
    {
        var request = new UpdateCategoryRequest("Name", new string('a', 51), null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Icon);
    }

    [Theory]
    [InlineData("#ABC")]
    [InlineData("#AABBCC")]
    public void Validate_ValidHexColor_Passes(string color)
    {
        var request = new UpdateCategoryRequest("Name", null, color);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Color);
    }

    [Theory]
    [InlineData("not-a-color")]
    [InlineData("#GGG")]
    public void Validate_InvalidHexColor_Fails(string color)
    {
        var request = new UpdateCategoryRequest("Name", null, color);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Color);
    }
}
