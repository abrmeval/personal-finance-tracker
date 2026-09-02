using Microsoft.Extensions.Logging;
using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
using Personal.FinanceTracker.Finance.Application.DTOs.Responses;
using Personal.FinanceTracker.Finance.Application.Interfaces;
using Personal.FinanceTracker.Finance.Domain.Entities;
using Personal.FinanceTracker.Finance.Domain.Interfaces;
using Personal.FinanceTracker.Shared.Constants;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Finance.Infrastructure.Services;

public sealed class TransactionService(
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    ILogger<TransactionService> logger) : ITransactionService
{
    public async Task<Result<PagedResult<TransactionResponse>>> GetAllAsync(
        Guid userId,
        TransactionQueryParams queryParams,
        CancellationToken ct = default)
    {
        var page = queryParams.Page < 1 ? 1 : queryParams.Page;
        var pageSize = queryParams.PageSize is < 1 or > 100 ? 20 : queryParams.PageSize;

        var transactions = await transactionRepository.GetPagedByUserAsync(
            userId, page, pageSize,
            queryParams.StartDate, queryParams.EndDate,
            queryParams.CategoryId, queryParams.Type, ct);

        var totalCount = await transactionRepository.CountByUserAsync(
            userId, queryParams.StartDate, queryParams.EndDate,
            queryParams.CategoryId, queryParams.Type, ct);

        var categoryIds = transactions
            .Where(t => t.CategoryId.HasValue)
            .Select(t => t.CategoryId!.Value)
            .Distinct()
            .ToList();

        var categoryNames = new Dictionary<Guid, string>();
        foreach (var categoryId in categoryIds)
        {
            var category = await categoryRepository.GetByIdAsync(categoryId, ct);
            if (category is not null)
                categoryNames[categoryId] = category.Name;
        }

        var items = transactions
            .Select(t => MapToResponse(t, t.CategoryId.HasValue ? categoryNames.GetValueOrDefault(t.CategoryId.Value) : null))
            .ToList();

        var result = new PagedResult<TransactionResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Result<PagedResult<TransactionResponse>>.Success(result);
    }

    public async Task<Result<TransactionResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var transaction = await transactionRepository.GetByUserAndIdAsync(userId, id, ct);
        if (transaction is null)
        {
            logger.LogWarning("Transaction {TransactionId} not found for user {UserId}", id, userId);
            return Result<TransactionResponse>.Failure(new(ApiErrorCode.TransactionNotFound, "Transaction not found."));
        }

        string? categoryName = null;
        if (transaction.CategoryId.HasValue)
        {
            var category = await categoryRepository.GetByIdAsync(transaction.CategoryId.Value, ct);
            categoryName = category?.Name;
        }

        return Result<TransactionResponse>.Success(MapToResponse(transaction, categoryName));
    }

    public async Task<Result<TransactionResponse>> CreateAsync(Guid userId, CreateTransactionRequest request, CancellationToken ct = default)
    {
        if (request.CategoryId.HasValue)
        {
            var categoryExists = await categoryRepository.ExistsByUserAndIdAsync(userId, request.CategoryId.Value, ct);
            if (!categoryExists)
            {
                logger.LogWarning("Transaction creation failed: category {CategoryId} not found for user {UserId}", request.CategoryId, userId);
                return Result<TransactionResponse>.Failure(new(ApiErrorCode.CategoryNotFound, "The specified category was not found."));
            }
        }

        var transaction = Transaction.Create(
            userId, request.Description, request.Amount, request.Type,
            request.Date, request.CategoryId, request.Notes);

        await transactionRepository.AddAsync(transaction, ct);
        await transactionRepository.SaveChangesAsync(ct);

        logger.LogInformation("Transaction {TransactionId} created for user {UserId}", transaction.Id, userId);

        string? categoryName = null;
        if (transaction.CategoryId.HasValue)
        {
            var category = await categoryRepository.GetByIdAsync(transaction.CategoryId.Value, ct);
            categoryName = category?.Name;
        }

        return Result<TransactionResponse>.Success(MapToResponse(transaction, categoryName));
    }

    public async Task<Result<TransactionResponse>> UpdateAsync(Guid userId, Guid id, UpdateTransactionRequest request, CancellationToken ct = default)
    {
        var transaction = await transactionRepository.GetByUserAndIdAsync(userId, id, ct);
        if (transaction is null)
        {
            logger.LogWarning("Transaction update failed: {TransactionId} not found for user {UserId}", id, userId);
            return Result<TransactionResponse>.Failure(new(ApiErrorCode.TransactionNotFound, "Transaction not found."));
        }

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await categoryRepository.ExistsByUserAndIdAsync(userId, request.CategoryId.Value, ct);
            if (!categoryExists)
            {
                logger.LogWarning("Transaction update failed: category {CategoryId} not found for user {UserId}", request.CategoryId, userId);
                return Result<TransactionResponse>.Failure(new(ApiErrorCode.CategoryNotFound, "The specified category was not found."));
            }
        }

        transaction.Update(
            request.Description, request.Amount, request.Type,
            request.Date, request.CategoryId, request.Notes);

        await transactionRepository.SaveChangesAsync(ct);

        logger.LogInformation("Transaction {TransactionId} updated by user {UserId}", transaction.Id, userId);

        string? categoryName = null;
        if (transaction.CategoryId.HasValue)
        {
            var category = await categoryRepository.GetByIdAsync(transaction.CategoryId.Value, ct);
            categoryName = category?.Name;
        }

        return Result<TransactionResponse>.Success(MapToResponse(transaction, categoryName));
    }

    public async Task<Result<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var transaction = await transactionRepository.GetByUserAndIdAsync(userId, id, ct);
        if (transaction is null)
        {
            logger.LogWarning("Transaction delete failed: {TransactionId} not found for user {UserId}", id, userId);
            return Result<bool>.Failure(new(ApiErrorCode.TransactionNotFound, "Transaction not found."));
        }

        await transactionRepository.DeleteAsync(transaction, ct);
        await transactionRepository.SaveChangesAsync(ct);

        logger.LogInformation("Transaction {TransactionId} deleted by user {UserId}", transaction.Id, userId);
        return Result<bool>.Success(true);
    }

    private static TransactionResponse MapToResponse(Transaction transaction, string? categoryName)
        => new(
            transaction.Id,
            transaction.Description,
            transaction.Amount,
            transaction.Type,
            transaction.Date,
            transaction.CategoryId,
            categoryName,
            transaction.Notes,
            transaction.CreatedAt,
            transaction.UpdatedAt);
}