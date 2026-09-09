using Microsoft.Extensions.Logging;
using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
using Personal.FinanceTracker.Finance.Application.DTOs.Responses;
using Personal.FinanceTracker.Finance.Application.Interfaces;
using Personal.FinanceTracker.Finance.Domain.Entities;
using Personal.FinanceTracker.Finance.Domain.Enums;
using Personal.FinanceTracker.Finance.Domain.Interfaces;
using Personal.FinanceTracker.Shared.Constants;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Finance.Infrastructure.Services;

public sealed class BudgetService(
    IBudgetRepository budgetRepository,
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository,
    ILogger<BudgetService> logger) : IBudgetService
{
    public async Task<Result<IReadOnlyList<BudgetWithSpendingResponse>>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        var budgets = await budgetRepository.GetAllByUserAsync(userId, ct);

        // Single category fetch — avoids a per-budget category query
        var categoryNames = (await categoryRepository.GetAllByUserAsync(userId, ct))
            .ToDictionary(c => c.Id, c => c.Name);

        var results = new List<BudgetWithSpendingResponse>(budgets.Count);
        foreach (var budget in budgets)
        {
            var spent = await GetSpendingForPeriodAsync(userId, budget.CategoryId, budget.Period, ct);
            results.Add(MapToWithSpending(budget, categoryNames.GetValueOrDefault(budget.CategoryId, "Unknown"), spent));
        }

        return Result<IReadOnlyList<BudgetWithSpendingResponse>>.Success(results);
    }

    public async Task<Result<BudgetWithSpendingResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var budget = await budgetRepository.GetByUserAndIdAsync(userId, id, ct);
        if (budget is null)
        {
            logger.LogWarning("Budget {BudgetId} not found for user {UserId}", id, userId);
            return Result<BudgetWithSpendingResponse>.Failure(new(ApiErrorCode.BudgetNotFound, "Budget not found."));
        }

        var spent = await GetSpendingForPeriodAsync(userId, budget.CategoryId, budget.Period, ct);
        var categoryName = await GetCategoryNameAsync(userId, budget.CategoryId, ct);
        return Result<BudgetWithSpendingResponse>.Success(MapToWithSpending(budget, categoryName, spent));
    }

    public async Task<Result<BudgetWithSpendingResponse>> CreateAsync(Guid userId, CreateBudgetRequest request, CancellationToken ct = default)
    {
        if (!await categoryRepository.ExistsByUserAndIdAsync(userId, request.CategoryId, ct))
        {
            logger.LogWarning("Budget creation failed: category {CategoryId} not found for user {UserId}", request.CategoryId, userId);
            return Result<BudgetWithSpendingResponse>.Failure(new(ApiErrorCode.CategoryNotFound, "Category not found."));
        }

        if (await budgetRepository.ExistsByUserAndCategoryAsync(userId, request.CategoryId, ct))
        {
            logger.LogWarning("Budget creation failed: a budget for category {CategoryId} already exists for user {UserId}", request.CategoryId, userId);
            return Result<BudgetWithSpendingResponse>.Failure(new(ApiErrorCode.DuplicateBudgetCategory, "A budget already exists for this category."));
        }

        var budget = Budget.Create(userId, request.CategoryId, request.Name, request.LimitAmount, request.Period);
        await budgetRepository.AddAsync(budget, ct);
        await budgetRepository.SaveChangesAsync(ct);

        logger.LogInformation("Budget {BudgetId} created for user {UserId}", budget.Id, userId);

        var spent = await GetSpendingForPeriodAsync(userId, budget.CategoryId, budget.Period, ct);
        var categoryName = await GetCategoryNameAsync(userId, budget.CategoryId, ct);
        return Result<BudgetWithSpendingResponse>.Success(MapToWithSpending(budget, categoryName, spent));
    }

    public async Task<Result<BudgetWithSpendingResponse>> UpdateAsync(Guid userId, Guid id, UpdateBudgetRequest request, CancellationToken ct = default)
    {
        var budget = await budgetRepository.GetByUserAndIdAsync(userId, id, ct);
        if (budget is null)
        {
            logger.LogWarning("Budget update failed: {BudgetId} not found for user {UserId}", id, userId);
            return Result<BudgetWithSpendingResponse>.Failure(new(ApiErrorCode.BudgetNotFound, "Budget not found."));
        }

        budget.Update(request.Name, request.LimitAmount, request.Period);
        await budgetRepository.SaveChangesAsync(ct);

        logger.LogInformation("Budget {BudgetId} updated by user {UserId}", budget.Id, userId);

        var spent = await GetSpendingForPeriodAsync(userId, budget.CategoryId, budget.Period, ct);
        var categoryName = await GetCategoryNameAsync(userId, budget.CategoryId, ct);
        return Result<BudgetWithSpendingResponse>.Success(MapToWithSpending(budget, categoryName, spent));
    }

    public async Task<Result<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var budget = await budgetRepository.GetByUserAndIdAsync(userId, id, ct);
        if (budget is null)
        {
            logger.LogWarning("Budget delete failed: {BudgetId} not found for user {UserId}", id, userId);
            return Result<bool>.Failure(new(ApiErrorCode.BudgetNotFound, "Budget not found."));
        }

        await budgetRepository.DeleteAsync(budget, ct);
        await budgetRepository.SaveChangesAsync(ct);

        logger.LogInformation("Budget {BudgetId} deleted by user {UserId}", budget.Id, userId);
        return Result<bool>.Success(true);
    }

    private async Task<string> GetCategoryNameAsync(Guid userId, Guid categoryId, CancellationToken ct)
    {
        var category = await categoryRepository.GetByUserAndIdAsync(userId, categoryId, ct);
        return category?.Name ?? "Unknown";
    }

    private async Task<decimal> GetSpendingForPeriodAsync(
        Guid userId,
        Guid categoryId,
        BudgetPeriod period,
        CancellationToken ct)
    {
        var (from, to) = GetPeriodRange(period);
        return await transactionRepository.GetTotalExpensesByCategoryAsync(userId, categoryId, from, to, ct);
    }

    private static (DateTime From, DateTime To) GetPeriodRange(BudgetPeriod period)
    {
        var now = DateTime.UtcNow;
        return period switch
        {
            BudgetPeriod.Daily => (now.Date, now.Date.AddDays(1).AddTicks(-1)),
            BudgetPeriod.Weekly => (now.Date.AddDays(-(int)now.DayOfWeek), now.Date.AddDays(7 - (int)now.DayOfWeek).AddTicks(-1)),
            BudgetPeriod.Monthly => (new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1).AddTicks(-1)),
            BudgetPeriod.Yearly => (new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(now.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(-1)),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };
    }

    private static BudgetWithSpendingResponse MapToWithSpending(Budget budget, string categoryName, decimal spent)
    {
        var remaining = budget.LimitAmount - spent;
        var percentage = budget.LimitAmount > 0
            ? Math.Round(spent / budget.LimitAmount * 100, 2)
            : 0m;

        return new BudgetWithSpendingResponse(
            Id: budget.Id,
            CategoryId: budget.CategoryId,
            CategoryName: categoryName,
            Name: budget.Name,
            LimitAmount: budget.LimitAmount,
            Period: budget.Period,
            SpentAmount: spent,
            RemainingAmount: remaining,
            PercentageUsed: percentage,
            IsOverBudget: spent > budget.LimitAmount,
            CreatedAt: budget.CreatedAt,
            UpdatedAt: budget.UpdatedAt);
    }
}
