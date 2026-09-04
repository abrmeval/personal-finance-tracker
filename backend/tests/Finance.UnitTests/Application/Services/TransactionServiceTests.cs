using Microsoft.Extensions.Logging;
using NSubstitute;
using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
using Personal.FinanceTracker.Finance.Domain.Entities;
using Personal.FinanceTracker.Finance.Domain.Enums;
using Personal.FinanceTracker.Finance.Domain.Interfaces;
using Personal.FinanceTracker.Finance.Infrastructure.Services;
using Personal.FinanceTracker.Shared.Constants;
using Personal.FinanceTracker.Shared.Models;

namespace Finance.UnitTests.Application.Services;

public class TransactionServiceTests
{
    private readonly ITransactionRepository _transactionRepo = Substitute.For<ITransactionRepository>();
    private readonly ICategoryRepository _categoryRepo = Substitute.For<ICategoryRepository>();
    private readonly ILogger<TransactionService> _logger = Substitute.For<ILogger<TransactionService>>();
    private readonly TransactionService _sut;
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    public TransactionServiceTests()
    {
        _sut = new TransactionService(_transactionRepo, _categoryRepo, _logger);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResult()
    {
        var query = new TransactionQueryParams { Page = 1, PageSize = 10 };
        var transactions = new List<Transaction>
        {
            Transaction.Create(UserId, "T1", 100m, TransactionType.Expense, DateTime.UtcNow, CategoryId),
            Transaction.Create(UserId, "T2", 200m, TransactionType.Income, DateTime.UtcNow)
        };
        _transactionRepo.GetPagedByUserAsync(UserId, 1, 10, null, null, null, null, Arg.Any<CancellationToken>()).Returns(transactions);
        _transactionRepo.CountByUserAsync(UserId, null, null, null, null, Arg.Any<CancellationToken>()).Returns(2);
        _categoryRepo.GetByIdAsync(CategoryId, Arg.Any<CancellationToken>()).Returns(Category.Create(UserId, "Food", null, null));

        var result = await _sut.GetAllAsync(UserId, query);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public async Task GetAllAsync_NormalizesInvalidPagination()
    {
        var query = new TransactionQueryParams { Page = 0, PageSize = 200 };
        _transactionRepo.GetPagedByUserAsync(UserId, 1, 20, null, null, null, null, Arg.Any<CancellationToken>()).Returns([]);
        _transactionRepo.CountByUserAsync(UserId, null, null, null, null, Arg.Any<CancellationToken>()).Returns(0);

        var result = await _sut.GetAllAsync(UserId, query);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Page);
        Assert.Equal(20, result.Value.PageSize);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTransaction_ReturnsTransaction()
    {
        var transaction = Transaction.Create(UserId, "T1", 100m, TransactionType.Expense, DateTime.UtcNow, CategoryId);
        _transactionRepo.GetByUserAndIdAsync(UserId, transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);
        _categoryRepo.GetByIdAsync(CategoryId, Arg.Any<CancellationToken>()).Returns(Category.Create(UserId, "Food", null, null));

        var result = await _sut.GetByIdAsync(UserId, transaction.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("T1", result.Value!.Description);
        Assert.Equal("Food", result.Value.CategoryName);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _transactionRepo.GetByUserAndIdAsync(UserId, id, Arg.Any<CancellationToken>()).Returns((Transaction?)null);

        var result = await _sut.GetByIdAsync(UserId, id);

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.TransactionNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesAndReturnsTransaction()
    {
        var request = new CreateTransactionRequest("T1", 100m, TransactionType.Expense, DateTime.UtcNow, null, null);

        var result = await _sut.CreateAsync(UserId, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("T1", result.Value!.Description);
        await _transactionRepo.Received(1).AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await _transactionRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithValidCategory_CreatesSuccessfully()
    {
        var request = new CreateTransactionRequest("T1", 100m, TransactionType.Expense, DateTime.UtcNow, CategoryId, null);
        _categoryRepo.ExistsByUserAndIdAsync(UserId, CategoryId, Arg.Any<CancellationToken>()).Returns(true);
        _categoryRepo.GetByIdAsync(CategoryId, Arg.Any<CancellationToken>()).Returns(Category.Create(UserId, "Food", null, null));

        var result = await _sut.CreateAsync(UserId, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(CategoryId, result.Value!.CategoryId);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCategory_ReturnsFailure()
    {
        var request = new CreateTransactionRequest("T1", 100m, TransactionType.Expense, DateTime.UtcNow, CategoryId, null);
        _categoryRepo.ExistsByUserAndIdAsync(UserId, CategoryId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CreateAsync(UserId, request);

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.CategoryNotFound, result.Error!.Code);
        await _transactionRepo.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ExistingTransaction_UpdatesAndReturnsTransaction()
    {
        var transaction = Transaction.Create(UserId, "Old", 50m, TransactionType.Expense, DateTime.UtcNow);
        var request = new UpdateTransactionRequest("New", 150m, TransactionType.Income, DateTime.UtcNow, null, null);
        _transactionRepo.GetByUserAndIdAsync(UserId, transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);

        var result = await _sut.UpdateAsync(UserId, transaction.Id, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("New", result.Value!.Description);
        Assert.Equal(150m, result.Value.Amount);
        Assert.Equal(TransactionType.Income, result.Value.Type);
        await _transactionRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        var request = new UpdateTransactionRequest("New", 100m, TransactionType.Expense, DateTime.UtcNow, null, null);
        _transactionRepo.GetByUserAndIdAsync(UserId, id, Arg.Any<CancellationToken>()).Returns((Transaction?)null);

        var result = await _sut.UpdateAsync(UserId, id, request);

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.TransactionNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidCategory_ReturnsFailure()
    {
        var transaction = Transaction.Create(UserId, "Old", 50m, TransactionType.Expense, DateTime.UtcNow);
        var request = new UpdateTransactionRequest("New", 100m, TransactionType.Expense, DateTime.UtcNow, CategoryId, null);
        _transactionRepo.GetByUserAndIdAsync(UserId, transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);
        _categoryRepo.ExistsByUserAndIdAsync(UserId, CategoryId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.UpdateAsync(UserId, transaction.Id, request);

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.CategoryNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task DeleteAsync_ExistingTransaction_DeletesAndReturnsTrue()
    {
        var transaction = Transaction.Create(UserId, "T1", 100m, TransactionType.Expense, DateTime.UtcNow);
        _transactionRepo.GetByUserAndIdAsync(UserId, transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);

        var result = await _sut.DeleteAsync(UserId, transaction.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        await _transactionRepo.Received(1).DeleteAsync(transaction, Arg.Any<CancellationToken>());
        await _transactionRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _transactionRepo.GetByUserAndIdAsync(UserId, id, Arg.Any<CancellationToken>()).Returns((Transaction?)null);

        var result = await _sut.DeleteAsync(UserId, id);

        Assert.True(result.IsFailure);
        Assert.Equal(ApiErrorCode.TransactionNotFound, result.Error!.Code);
    }
}
