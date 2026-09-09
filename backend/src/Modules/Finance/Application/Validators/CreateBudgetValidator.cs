using FluentValidation;
using Personal.FinanceTracker.Finance.Application.DTOs.Requests;

namespace Personal.FinanceTracker.Finance.Application.Validators;

public sealed class CreateBudgetValidator : AbstractValidator<CreateBudgetRequest>
{
    public CreateBudgetValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Budget name is required.")
            .MaximumLength(150).WithMessage("Budget name cannot exceed 150 characters.");

        RuleFor(x => x.LimitAmount)
            .GreaterThan(0).WithMessage("Limit amount must be greater than zero.")
            .LessThanOrEqualTo(1_000_000_000).WithMessage("Limit amount is unreasonably large.");

        RuleFor(x => x.Period)
            .IsInEnum().WithMessage("Invalid budget period.");
    }
}
