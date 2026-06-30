using Personal.FinanceTracker.Shared.Abstractions;

namespace Personal.FinanceTracker.Finance.Domain.Entities;

public sealed class Category : Entity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Icon { get; private set; }
    public string? Color { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Category() { }

    public static Category Create(
        Guid userId,
        string name,
        string? icon = null,
        string? color = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Category name cannot exceed 100 characters.", nameof(name));

        return new Category
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            Icon = string.IsNullOrWhiteSpace(icon) ? null : icon.Trim(),
            Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? icon = null, string? color = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Category name cannot exceed 100 characters.", nameof(name));

        Name = name.Trim();
        Icon = string.IsNullOrWhiteSpace(icon) ? null : icon.Trim();
        Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}