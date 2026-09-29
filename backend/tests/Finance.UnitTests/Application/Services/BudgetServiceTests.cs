using Microsoft.Extensions.Logging;
using NSubstitute;
using Personal.FinanceTracker.Finance.Domain.Entities;
using Personal.FinanceTracker.Finance.Domain.Enums;
using Personal.FinanceTracker.Finance.Domain.Interfaces;
using Personal.FinanceTracker.Finance.Infrastructure.Services;

namespace Finance.UnitTests.Application.Services;

public sealed class BudgetServiceTests
{
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly ILogger<BudgetService> _logger = Substitute.For<ILogger<BudgetService>>();

    private BudgetService CreateSut() => new(
        _budgetRepository,
        _categoryRepository,
        _transactionRepository,
        _logger);

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task GetBudgetsNearLimitAsync_WithThresholdOutOfRange_ReturnsInvalidReportParameters(decimal threshold)
    {
        var result = await CreateSut().GetBudgetsNearLimitAsync(threshold);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("INVALID_REPORT_PARAMETERS");
        await _budgetRepository.DidNotReceive().GetAllActiveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBudgetsNearLimitAsync_WithBudgetsAboveThreshold_ReturnsOnlyThoseBudgets()
    {
        var userId = Guid.NewGuid();
        var includedCategoryId = Guid.NewGuid();
        var excludedCategoryId = Guid.NewGuid();
        var included = Budget.Create(userId, includedCategoryId, "Near limit", 100m, BudgetPeriod.Monthly);
        var excluded = Budget.Create(userId, excludedCategoryId, "Healthy", 100m, BudgetPeriod.Monthly);
        _budgetRepository.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns([included, excluded]);
        _categoryRepository.GetAllByUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var near = Category.Create(userId, "Near", null, null);
                var healthy = Category.Create(userId, "Healthy", null, null);
                return new[] { near, healthy }
                    .Select(category => category.Id == includedCategoryId
                        ? Category.Create(userId, "Near", null, null)
                        : category)
                    .ToList();
            });
        _transactionRepository.GetTotalExpensesByCategoryAsync(
                userId,
                includedCategoryId,
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(90m);
        _transactionRepository.GetTotalExpensesByCategoryAsync(
                userId,
                excludedCategoryId,
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(20m);

        var result = await CreateSut().GetBudgetsNearLimitAsync(80m);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value![0].Name.Should().Be("Near limit");
        result.Value[0].PercentageUsed.Should().Be(90m);
    }

    [Fact]
    public async Task GetBudgetsNearLimitAsync_WithNoActiveBudgets_ReturnsEmptyList()
    {
        _budgetRepository.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateSut().GetBudgetsNearLimitAsync(80m);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _categoryRepository.DidNotReceive().GetAllByUserAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
