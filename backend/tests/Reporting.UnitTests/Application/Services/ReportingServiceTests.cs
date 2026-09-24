using Microsoft.Extensions.Logging;
using NSubstitute;
using Personal.FinanceTracker.Finance.Domain.Interfaces;
using Personal.FinanceTracker.Finance.Domain.Models;
using Personal.FinanceTracker.Reporting.Domain.Entities;
using Personal.FinanceTracker.Reporting.Domain.Interfaces;
using Personal.FinanceTracker.Reporting.Infrastructure.Services;

namespace Reporting.UnitTests.Application.Services;

public sealed class ReportingServiceTests
{
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly IMonthlySummaryRepository _monthlySummaryRepository = Substitute.For<IMonthlySummaryRepository>();
    private readonly ILogger<ReportingService> _logger = Substitute.For<ILogger<ReportingService>>();

    private ReportingService CreateSut() => new(_transactionRepository, _monthlySummaryRepository, _logger);

    [Fact]
    public async Task GenerateMonthlyReportsAsync_WhenSummaryMissing_CreatesSummary()
    {
        var userId = Guid.NewGuid();
        _transactionRepository.GetUserMonthlyTotalsAsync(2026, 8, Arg.Any<CancellationToken>())
            .Returns([new UserMonthlyTotals(userId, 2000m, 1200m)]);
        _monthlySummaryRepository.GetByUserAndPeriodAsync(userId, 2026, 8, Arg.Any<CancellationToken>())
            .Returns((MonthlySummary?)null);

        var result = await CreateSut().GenerateMonthlyReportsAsync(2026, 8);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        await _monthlySummaryRepository.Received(1).AddAsync(
            Arg.Is<MonthlySummary>(summary =>
                summary.UserId == userId &&
                summary.Year == 2026 &&
                summary.Month == 8 &&
                summary.TotalIncome == 2000m &&
                summary.TotalExpenses == 1200m),
            Arg.Any<CancellationToken>());
        await _monthlySummaryRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateMonthlyReportsAsync_WhenSummaryExists_UpdatesSummary()
    {
        var userId = Guid.NewGuid();
        var existing = MonthlySummary.Create(userId, 2026, 8, 1000m, 900m);
        _transactionRepository.GetUserMonthlyTotalsAsync(2026, 8, Arg.Any<CancellationToken>())
            .Returns([new UserMonthlyTotals(userId, 2400m, 1000m)]);
        _monthlySummaryRepository.GetByUserAndPeriodAsync(userId, 2026, 8, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await CreateSut().GenerateMonthlyReportsAsync(2026, 8);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        existing.TotalIncome.Should().Be(2400m);
        existing.TotalExpenses.Should().Be(1000m);
        existing.NetAmount.Should().Be(1400m);
        await _monthlySummaryRepository.DidNotReceive().AddAsync(
            Arg.Any<MonthlySummary>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateMonthlyReportsAsync_WithInvalidMonth_ReturnsInvalidReportParameters()
    {
        var result = await CreateSut().GenerateMonthlyReportsAsync(2026, 13);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("INVALID_REPORT_PARAMETERS");
        await _transactionRepository.DidNotReceive()
            .GetUserMonthlyTotalsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateMonthlyReportsAsync_WithNoTransactions_PersistsNothingAndReturnsZero()
    {
        _transactionRepository.GetUserMonthlyTotalsAsync(2026, 8, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await CreateSut().GenerateMonthlyReportsAsync(2026, 8);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
        await _monthlySummaryRepository.DidNotReceive().AddAsync(
            Arg.Any<MonthlySummary>(), Arg.Any<CancellationToken>());
        await _monthlySummaryRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
