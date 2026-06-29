using FluentValidation;
using Personal.FinanceTracker.Finance.Application.DTOs.Requests;

namespace Personal.FinanceTracker.Finance.Application.Validators;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚ\s'.,&()-*]+$").WithMessage("Category name contains invalid characters.");

        RuleFor(x => x.Icon)
            .MaximumLength(50).WithMessage("Icon cannot exceed 50 characters.");

        RuleFor(x => x.Color)
            .MaximumLength(20).WithMessage("Color cannot exceed 20 characters.")
            .Matches(@"^#(?:[0-9a-fA-F]{3}){1,2}$").WithMessage("Color must be a valid hex color code.");
    }
}