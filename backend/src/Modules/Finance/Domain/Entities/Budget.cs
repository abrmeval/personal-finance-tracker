using Personal.FinanceTracker.Finance.Domain.Enums;
using Personal.FinanceTracker.Shared.Abstractions;

namespace Personal.FinanceTracker.Finance.Domain.Entities;

public sealed class Budget : Entity
{
    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal LimitAmount { get; private set; }
    public BudgetPeriod Period { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Budget() { }

    public static Budget Create(
        Guid userId,
        Guid categoryId,
        string name,
        decimal limitAmount,
        BudgetPeriod period)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("Category ID is required.", nameof(categoryId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Budget name is required.", nameof(name));

        if (name.Length > 150)
            throw new ArgumentException("Budget name cannot exceed 150 characters.", nameof(name));

        if (limitAmount <= 0)
            throw new ArgumentException("Limit amount must be greater than zero.", nameof(limitAmount));

        return new Budget
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CategoryId = categoryId,
            Name = name.Trim(),
            LimitAmount = limitAmount,
            Period = period,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(Guid categoryId, string name, decimal limitAmount, BudgetPeriod period)
    {
        if (categoryId == Guid.Empty)
            throw new ArgumentException("Category ID is required.", nameof(categoryId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Budget name is required.", nameof(name));

        if (name.Length > 150)
            throw new ArgumentException("Budget name cannot exceed 150 characters.", nameof(name));

        if (limitAmount <= 0)
            throw new ArgumentException("Limit amount must be greater than zero.", nameof(limitAmount));

        CategoryId = categoryId;
        Name = name.Trim();
        LimitAmount = limitAmount;
        Period = period;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
