using FluentValidation;
using Personal.FinanceTracker.Finance.Application.DTOs.Requests;

namespace Personal.FinanceTracker.Finance.Application.Validators;

public sealed class UpdateTransactionValidator : AbstractValidator<UpdateTransactionRequest>
{
    public UpdateTransactionValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
            .Matches(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚ\s'.,&()-*]+$").WithMessage("Description contains invalid characters.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.")
            .LessThanOrEqualTo(1_000_000_000).WithMessage("Amount is unreasonably large.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid transaction type.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters.")
            .Matches(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚ\s'.,&()-*]+$").WithMessage("Notes contains invalid characters.");
    }
}