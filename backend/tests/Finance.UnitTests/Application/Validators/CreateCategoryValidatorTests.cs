using FluentValidation.TestHelper;
using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
using Personal.FinanceTracker.Finance.Application.Validators;

namespace Finance.UnitTests.Application.Validators;

public class CreateCategoryValidatorTests
{
    private readonly CreateCategoryValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var request = new CreateCategoryRequest("Groceries", "🛒", "#FF5733");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var request = new CreateCategoryRequest("", null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceeds100Characters_Fails()
    {
        var request = new CreateCategoryRequest(new string('a', 101), null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("Groceries&Food")]
    [InlineData("Test'Category")]
    [InlineData("Café")]
    public void Validate_NameWithValidSpecialCharacters_Passes(string name)
    {
        var request = new CreateCategoryRequest(name, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("Groceries@Home")]
    [InlineData("Test<Category>")]
    [InlineData("Test/Category")]
    public void Validate_NameWithInvalidCharacters_Fails(string name)
    {
        var request = new CreateCategoryRequest(name, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_IconExceeds50Characters_Fails()
    {
        var request = new CreateCategoryRequest("Name", new string('a', 51), null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Icon);
    }

    [Theory]
    [InlineData("#FFF")]
    [InlineData("#FFFFFF")]
    [InlineData("#ff5733")]
    [InlineData("#FF5733")]
    public void Validate_ValidHexColor_Passes(string color)
    {
        var request = new CreateCategoryRequest("Name", null, color);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Color);
    }

    [Theory]
    [InlineData("FFF")]
    [InlineData("#GGGGGG")]
    [InlineData("#FFFFFFF")]
    [InlineData("#FF")]
    public void Validate_InvalidHexColor_Fails(string color)
    {
        var request = new CreateCategoryRequest("Name", null, color);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Color);
    }

    [Fact]
    public void Validate_ColorExceeds20Characters_Fails()
    {
        var request = new CreateCategoryRequest("Name", null, new string('a', 21));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Color);
    }
}
