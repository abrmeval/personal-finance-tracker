using FluentValidation.TestHelper;
using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
using Personal.FinanceTracker.Finance.Application.Validators;
using Personal.FinanceTracker.Finance.Domain.Enums;

namespace Finance.UnitTests.Application.Validators;

public class CreateTransactionValidatorTests
{
    private readonly CreateTransactionValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var request = new CreateTransactionRequest(
            "Grocery shopping", 150.50m, TransactionType.Expense,
            DateTime.UtcNow, Guid.NewGuid(), "Weekly groceries");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyDescription_Fails()
    {
        var request = new CreateTransactionRequest(
            "", 100m, TransactionType.Expense, DateTime.UtcNow, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_DescriptionExceeds500Characters_Fails()
    {
        var request = new CreateTransactionRequest(
            new string('a', 501), 100m, TransactionType.Expense, DateTime.UtcNow, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Theory]
    [InlineData("Desc@Invalid")]
    [InlineData("Desc<Invalid>")]
    public void Validate_DescriptionWithInvalidCharacters_Fails(string description)
    {
        var request = new CreateTransactionRequest(
            description, 100m, TransactionType.Expense, DateTime.UtcNow, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_AmountZero_Fails()
    {
        var request = new CreateTransactionRequest(
            "Desc", 0m, TransactionType.Expense, DateTime.UtcNow, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_AmountNegative_Fails()
    {
        var request = new CreateTransactionRequest(
            "Desc", -10m, TransactionType.Expense, DateTime.UtcNow, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_AmountTooLarge_Fails()
    {
        var request = new CreateTransactionRequest(
            "Desc", 1_000_000_001m, TransactionType.Expense, DateTime.UtcNow, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_InvalidEnumValue_Fails()
    {
        var request = new CreateTransactionRequest(
            "Desc", 100m, (TransactionType)99, DateTime.UtcNow, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void Validate_EmptyDate_Fails()
    {
        var request = new CreateTransactionRequest(
            "Desc", 100m, TransactionType.Expense, default, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Validate_NotesExceeds2000Characters_Fails()
    {
        var request = new CreateTransactionRequest(
            "Desc", 100m, TransactionType.Expense, DateTime.UtcNow, null, new string('a', 2001));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Theory]
    [InlineData("Notes@Invalid")]
    public void Validate_NotesWithInvalidCharacters_Fails(string notes)
    {
        var request = new CreateTransactionRequest(
            "Desc", 100m, TransactionType.Expense, DateTime.UtcNow, null, notes);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }
}
