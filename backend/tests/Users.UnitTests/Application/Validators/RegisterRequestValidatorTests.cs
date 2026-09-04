using FluentValidation.TestHelper;
using Personal.FinanceTracker.Users.Application.DTOs.Requests;
using Personal.FinanceTracker.Users.Application.Validators;

namespace Users.UnitTests.Application.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var request = new RegisterRequest(
            "test@example.com", "Password123.", "John", "Doe");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyEmail_Fails()
    {
        var request = new RegisterRequest("", "Password123!", "John", "Doe");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_InvalidEmail_Fails()
    {
        var request = new RegisterRequest("not-an-email", "Password123!", "John", "Doe");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_EmailExceeds256Characters_Fails()
    {
        var email = new string('a', 250) + "@example.com";
        var request = new RegisterRequest(email, "Password123!", "John", "Doe");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_EmptyPassword_Fails()
    {
        var request = new RegisterRequest("test@example.com", "", "John", "Doe");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordTooShort_Fails()
    {
        var request = new RegisterRequest("test@example.com", "Pass1!", "John", "Doe");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordWithoutUppercase_Fails()
    {
        var request = new RegisterRequest("test@example.com", "password123!", "John", "Doe");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordWithoutLowercase_Fails()
    {
        var request = new RegisterRequest("test@example.com", "PASSWORD123!", "John", "Doe");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordWithoutDigit_Fails()
    {
        var request = new RegisterRequest("test@example.com", "Password!!!", "John", "Doe");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordWithoutSpecialCharacter_Fails()
    {
        var request = new RegisterRequest("test@example.com", "Password123", "John", "Doe");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_EmptyFirstName_Fails()
    {
        var request = new RegisterRequest("test@example.com", "Password123!", "", "Doe");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Validate_FirstNameExceeds100Characters_Fails()
    {
        var request = new RegisterRequest("test@example.com", "Password123!", new string('a', 101), "Doe");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Validate_EmptyLastName_Fails()
    {
        var request = new RegisterRequest("test@example.com", "Password123!", "John", "");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Validate_LastNameExceeds100Characters_Fails()
    {
        var request = new RegisterRequest("test@example.com", "Password123!", "John", new string('a', 101));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }
}
