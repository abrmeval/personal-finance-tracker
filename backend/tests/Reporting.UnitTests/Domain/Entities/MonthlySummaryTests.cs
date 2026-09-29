using Personal.FinanceTracker.Reporting.Domain.Entities;

namespace Reporting.UnitTests.Domain.Entities;

public sealed class MonthlySummaryTests
{
    [Fact]
    public void Create_WithValidInputs_SetsNetAmountAndDates()
    {
        var userId = Guid.NewGuid();

        var summary = MonthlySummary.Create(userId, 2026, 9, 2500m, 1800m);

        summary.Id.Should().NotBe(Guid.Empty);
        summary.UserId.Should().Be(userId);
        summary.Year.Should().Be(2026);
        summary.Month.Should().Be(9);
        summary.TotalIncome.Should().Be(2500m);
        summary.TotalExpenses.Should().Be(1800m);
        summary.NetAmount.Should().Be(700m);
        summary.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public void Create_WithInvalidYear_ThrowsArgumentException()
    {
        var act = () => MonthlySummary.Create(Guid.NewGuid(), 1999, 9, 1m, 1m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithInvalidMonth_ThrowsArgumentException()
    {
        var act = () => MonthlySummary.Create(Guid.NewGuid(), 2026, 13, 1m, 1m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNegativeIncome_ThrowsArgumentException()
    {
        var act = () => MonthlySummary.Create(Guid.NewGuid(), 2026, 9, -1m, 1m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNegativeExpenses_ThrowsArgumentException()
    {
        var act = () => MonthlySummary.Create(Guid.NewGuid(), 2026, 9, 1m, -1m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        var act = () => MonthlySummary.Create(Guid.Empty, 2026, 9, 1m, 1m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_RecalculatesNetAmount()
    {
        var summary = MonthlySummary.Create(Guid.NewGuid(), 2026, 9, 2500m, 1800m);

        summary.Update(3000m, 900m);

        summary.TotalIncome.Should().Be(3000m);
        summary.TotalExpenses.Should().Be(900m);
        summary.NetAmount.Should().Be(2100m);
        summary.UpdatedAt.Should().NotBeNull();
    }
}
