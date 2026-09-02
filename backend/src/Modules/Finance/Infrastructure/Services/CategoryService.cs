using Microsoft.Extensions.Logging;
using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
using Personal.FinanceTracker.Finance.Application.DTOs.Responses;
using Personal.FinanceTracker.Finance.Application.Interfaces;
using Personal.FinanceTracker.Finance.Domain.Entities;
using Personal.FinanceTracker.Finance.Domain.Interfaces;
using Personal.FinanceTracker.Shared.Constants;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Finance.Infrastructure.Services;

public sealed class CategoryService(
    ICategoryRepository repository,
    ILogger<CategoryService> logger) : ICategoryService
{
    public async Task<Result<IReadOnlyList<CategoryResponse>>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        var categories = await repository.GetAllByUserAsync(userId, ct);
        var response = categories.Select(MapToResponse).ToList();
        return Result<IReadOnlyList<CategoryResponse>>.Success(response);
    }

    public async Task<Result<CategoryResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var category = await repository.GetByUserAndIdAsync(userId, id, ct);
        if (category is null)
        {
            logger.LogWarning("Category {CategoryId} not found for user {UserId}", id, userId);
            return Result<CategoryResponse>.Failure(new(ApiErrorCode.CategoryNotFound, "Category not found."));
        }

        return Result<CategoryResponse>.Success(MapToResponse(category));
    }

    public async Task<Result<CategoryResponse>> CreateAsync(Guid userId, CreateCategoryRequest request, CancellationToken ct = default)
    {
        if (await repository.ExistsByUserAndNameAsync(userId, request.Name, ct))
        {
            logger.LogWarning("Category creation failed: name '{Name}' already exists for user {UserId}", request.Name, userId);
            return Result<CategoryResponse>.Failure(new(ApiErrorCode.DuplicateCategoryName, "A category with this name already exists."));
        }

        var category = Category.Create(userId, request.Name, request.Icon, request.Color);
        await repository.AddAsync(category, ct);
        await repository.SaveChangesAsync(ct);

        logger.LogInformation("Category {CategoryId} created for user {UserId}", category.Id, userId);
        return Result<CategoryResponse>.Success(MapToResponse(category));
    }

    public async Task<Result<CategoryResponse>> UpdateAsync(Guid userId, Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await repository.GetByUserAndIdAsync(userId, id, ct);
        if (category is null)
        {
            logger.LogWarning("Category update failed: {CategoryId} not found for user {UserId}", id, userId);
            return Result<CategoryResponse>.Failure(new(ApiErrorCode.CategoryNotFound, "Category not found."));
        }

        if (await repository.ExistsByUserAndNameAsync(userId, request.Name, ct) && category.Name != request.Name)
        {
            logger.LogWarning("Category update failed: name '{Name}' already exists for user {UserId}", request.Name, userId);
            return Result<CategoryResponse>.Failure(new(ApiErrorCode.DuplicateCategoryName, "A category with this name already exists."));
        }

        category.Update(request.Name, request.Icon, request.Color);
        await repository.SaveChangesAsync(ct);

        logger.LogInformation("Category {CategoryId} updated by user {UserId}", category.Id, userId);
        return Result<CategoryResponse>.Success(MapToResponse(category));
    }

    public async Task<Result<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var category = await repository.GetByUserAndIdAsync(userId, id, ct);
        if (category is null)
        {
            logger.LogWarning("Category delete failed: {CategoryId} not found for user {UserId}", id, userId);
            return Result<bool>.Failure(new(ApiErrorCode.CategoryNotFound, "Category not found."));
        }

        await repository.DeleteAsync(category, ct);
        await repository.SaveChangesAsync(ct);

        logger.LogInformation("Category {CategoryId} deleted by user {UserId}", category.Id, userId);
        return Result<bool>.Success(true);
    }

    private static CategoryResponse MapToResponse(Category category)
        => new(category.Id, category.Name, category.Icon, category.Color, category.CreatedAt, category.UpdatedAt);
}