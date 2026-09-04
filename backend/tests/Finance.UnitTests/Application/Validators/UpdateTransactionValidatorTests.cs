using FluentValidation.TestHelper;
using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
using Personal.FinanceTracker.Finance.Application.Validators;
using Personal.FinanceTracker.Finance.Domain.Enums;

namespace Finance.UnitTests.Application.Validators;

public class UpdateTransactionValidatorTests
{
    private readonly UpdateTransactionValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var request = new UpdateTransactionRequest(
            "Updated shopping", 200m, TransactionType.Income,
            DateTime.UtcNow, Guid.NewGuid(), "Updated notes");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyDescription_Fails()
    {
        var request = new UpdateTransactionRequest(
            "", 100m, TransactionType.Expense, DateTime.UtcNow, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_AmountZero_Fails()
    {
        var request = new UpdateTransactionRequest(
            "Desc", 0m, TransactionType.Expense, DateTime.UtcNow, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_InvalidEnumValue_Fails()
    {
        var request = new UpdateTransactionRequest(
            "Desc", 100m, (TransactionType)99, DateTime.UtcNow, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void Validate_NotesExceeds2000Characters_Fails()
    {
        var request = new UpdateTransactionRequest(
            "Desc", 100m, TransactionType.Expense, DateTime.UtcNow, null, new string('a', 2001));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }
}
