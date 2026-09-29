using Microsoft.Extensions.Logging;
using NSubstitute;
using Personal.FinanceTracker.Finance.Domain.Interfaces;
using Personal.FinanceTracker.Finance.Domain.Models;
using Personal.FinanceTracker.Reporting.Infrastructure.Services;

namespace Reporting.UnitTests.Application.Services;

public sealed class DashboardServiceTests
{
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly ILogger<DashboardService> _logger = Substitute.For<ILogger<DashboardService>>();

    private DashboardService CreateSut() => new(_transactionRepository, _categoryRepository, _logger);

    [Fact]
    public async Task GetSummaryAsync_WithMonthAndAllTimeTotals_ComputesBalanceAndMonthlyTotals()
    {
        var userId = Guid.NewGuid();
        _transactionRepository.GetTotalsByTypeAsync(
                userId,
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var from = (DateTime?)callInfo[1];
                return from is null
                    ? new TransactionTypeTotals(1000m, 400m)
                    : new TransactionTypeTotals(500m, 300m);
            });

        var result = await CreateSut().GetSummaryAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalBalance.Should().Be(600m);
        result.Value.MonthlyIncome.Should().Be(500m);
        result.Value.MonthlyExpenses.Should().Be(300m);
        await _transactionRepository.Received(2)
            .GetTotalsByTypeAsync(userId, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetIncomeVsExpensesAsync_WithNullMonths_DefaultsTo6ContiguousRows()
    {
        var now = DateTime.UtcNow;
        var currentMonth = new MonthlyTotals(now.Year, now.Month, 100m, 50m);
        _transactionRepository.GetMonthlyTotalsAsync(
                Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([currentMonth]);

        var result = await CreateSut().GetIncomeVsExpensesAsync(Guid.NewGuid(), null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(6);
        result.Value!.Select(m => (m.Year, m.Month)).Should().BeInAscendingOrder();
        result.Value[^1].Income.Should().Be(100m);
        result.Value[^1].Expenses.Should().Be(50m);
        result.Value!.Take(5).Should().AllSatisfy(month =>
        {
            month.Income.Should().Be(0m);
            month.Expenses.Should().Be(0m);
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    public async Task GetIncomeVsExpensesAsync_WithMonthsOutOfRange_ReturnsInvalidReportParameters(int months)
    {
        var result = await CreateSut().GetIncomeVsExpensesAsync(Guid.NewGuid(), months);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("INVALID_REPORT_PARAMETERS");
        await _transactionRepository.DidNotReceive()
            .GetMonthlyTotalsAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCategoryBreakdownAsync_WithMissingDates_ReturnsInvalidReportParameters()
    {
        var result = await CreateSut().GetCategoryBreakdownAsync(Guid.NewGuid(), null, DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("INVALID_REPORT_PARAMETERS");
    }

    [Fact]
    public async Task GetCategoryBreakdownAsync_WithStartAfterEnd_ReturnsInvalidReportParameters()
    {
        var result = await CreateSut().GetCategoryBreakdownAsync(
            Guid.NewGuid(),
            new DateTime(2026, 9, 20),
            new DateTime(2026, 9, 1));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("INVALID_REPORT_PARAMETERS");
    }

    [Fact]
    public async Task GetCategoryBreakdownAsync_WithUnknownCategoryId_FallsBackToUnknownName()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        _transactionRepository.GetExpenseTotalsByCategoryAsync(
                userId,
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns([new CategoryExpenseTotal(categoryId, 123m)]);
        _categoryRepository.GetAllByUserAsync(userId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateSut().GetCategoryBreakdownAsync(
            userId,
            new DateTime(2026, 9, 1),
            new DateTime(2026, 9, 15));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                CategoryId = categoryId,
                CategoryName = "Unknown",
                Total = 123m
            });
    }

    [Fact]
    public async Task GetCategoryBreakdownAsync_NormalizesEndDateToEndOfDay()
    {
        var userId = Guid.NewGuid();
        DateTime capturedFrom = default;
        DateTime capturedTo = default;
        _transactionRepository.GetExpenseTotalsByCategoryAsync(
                userId,
                Arg.Do<DateTime>(value => capturedFrom = value),
                Arg.Do<DateTime>(value => capturedTo = value),
                Arg.Any<CancellationToken>())
            .Returns([]);
        _categoryRepository.GetAllByUserAsync(userId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateSut().GetCategoryBreakdownAsync(
            userId,
            new DateTime(2026, 9, 1),
            new DateTime(2026, 9, 15));

        result.IsSuccess.Should().BeTrue();
        capturedFrom.Kind.Should().Be(DateTimeKind.Utc);
        capturedFrom.Should().Be(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));
        capturedTo.Kind.Should().Be(DateTimeKind.Utc);
        capturedTo.Should().Be(new DateTime(2026, 9, 15, 23, 59, 59, 999, DateTimeKind.Utc).AddTicks(9_999));
    }
}
