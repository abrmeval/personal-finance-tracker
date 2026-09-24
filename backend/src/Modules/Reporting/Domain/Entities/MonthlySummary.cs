using Personal.FinanceTracker.Shared.Abstractions;

namespace Personal.FinanceTracker.Reporting.Domain.Entities;

public sealed class MonthlySummary : Entity
{
    public Guid UserId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal TotalIncome { get; private set; }
    public decimal TotalExpenses { get; private set; }
    public decimal NetAmount { get; private set; }

    private MonthlySummary() { }

    public static MonthlySummary Create(
        Guid userId,
        int year,
        int month,
        decimal totalIncome,
        decimal totalExpenses)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (year is < 2000 or > 2100)
            throw new ArgumentException("Year is out of supported range.", nameof(year));

        if (month is < 1 or > 12)
            throw new ArgumentException("Month must be between 1 and 12.", nameof(month));

        if (totalIncome < 0)
            throw new ArgumentException("Total income cannot be negative.", nameof(totalIncome));

        if (totalExpenses < 0)
            throw new ArgumentException("Total expenses cannot be negative.", nameof(totalExpenses));

        return new MonthlySummary
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Year = year,
            Month = month,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            NetAmount = totalIncome - totalExpenses,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(decimal totalIncome, decimal totalExpenses)
    {
        if (totalIncome < 0)
            throw new ArgumentException("Total income cannot be negative.", nameof(totalIncome));

        if (totalExpenses < 0)
            throw new ArgumentException("Total expenses cannot be negative.", nameof(totalExpenses));

        TotalIncome = totalIncome;
        TotalExpenses = totalExpenses;
        NetAmount = totalIncome - totalExpenses;
        UpdatedAt = DateTime.UtcNow;
    }
}
