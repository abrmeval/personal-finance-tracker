using FluentValidation;
using Personal.FinanceTracker.Users.Application.DTOs.Requests;
namespace Personal.FinanceTracker.Users.Application.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.")
            .EmailAddress().WithMessage("Email must be a valid email address.");
            
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}