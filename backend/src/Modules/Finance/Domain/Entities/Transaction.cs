using Personal.FinanceTracker.Finance.Domain.Enums;
using Personal.FinanceTracker.Shared.Abstractions;

namespace Personal.FinanceTracker.Finance.Domain.Entities;

public sealed class Transaction : Entity
{
    public Guid UserId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public DateTime Date { get; private set; }
    public string? Notes { get; private set; }

    private Transaction() { }

    public static Transaction Create(
        Guid userId,
        string description,
        decimal amount,
        TransactionType type,
        DateTime date,
        Guid? categoryId = null,
        string? notes = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        if (description.Length > 500)
            throw new ArgumentException("Description cannot exceed 500 characters.", nameof(description));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        if (date == default)
            throw new ArgumentException("A valid date is required.", nameof(date));

        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CategoryId = categoryId,
            Description = description.Trim(),
            Amount = amount,
            Type = type,
            Date = date.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
                : date.ToUniversalTime(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string description,
        decimal amount,
        TransactionType type,
        DateTime date,
        Guid? categoryId = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        if (description.Length > 500)
            throw new ArgumentException("Description cannot exceed 500 characters.", nameof(description));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        if (date == default)
            throw new ArgumentException("A valid date is required.", nameof(date));

        Description = description.Trim();
        Amount = amount;
        Type = type;
        Date = date.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(date, DateTimeKind.Utc)
            : date.ToUniversalTime();
        CategoryId = categoryId;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}