using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
using Personal.FinanceTracker.Finance.Application.DTOs.Responses;
using Personal.FinanceTracker.Finance.Application.Interfaces;
using Personal.FinanceTracker.Shared.Extensions;
using Personal.FinanceTracker.Shared.Filters;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Finance.Api.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories")
            .WithTags("Categories")
            .RequireAuthorization();

        group.MapGet("/", GetAllAsync)
            .WithName("GetCategories")
            .WithDescription("Get all categories for the authenticated user.");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetCategoryById")
            .WithDescription("Get a single category by ID.");

        group.MapPost("/", CreateAsync)
            .WithName("CreateCategory")
            .WithDescription("Create a new category.")
            .AddEndpointFilter<ValidationFilter<CreateCategoryRequest>>();

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateCategory")
            .WithDescription("Update an existing category.")
            .AddEndpointFilter<ValidationFilter<UpdateCategoryRequest>>();

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteCategory")
            .WithDescription("Soft-delete a category. It is excluded from lists and lookups; transactions keep their category reference.");

        return app;
    }

    private static async Task<Ok<ApiResponse<IReadOnlyList<CategoryResponse>>>> GetAllAsync(
        ClaimsPrincipal user,
        ICategoryService categoryService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await categoryService.GetAllAsync(userId, ct);

        return TypedResults.Ok(new ApiResponse<IReadOnlyList<CategoryResponse>>
        {
            IsOk = true,
            Data = result.Value,
            StatusCode = StatusCodes.Status200OK,
            CodeText = "OK"
        });
    }

    private static async Task<Results<Ok<ApiResponse<CategoryResponse>>, NotFound<ApiResponse<CategoryResponse>>>> GetByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        ICategoryService categoryService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await categoryService.GetByIdAsync(userId, id, ct);

        if (result.IsFailure)
            return TypedResults.NotFound(new ApiResponse<CategoryResponse>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Category Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = result.Error?.Description,
                },
                StatusCode = StatusCodes.Status404NotFound,
                CodeText = "NOT_FOUND"
            });

        return TypedResults.Ok(new ApiResponse<CategoryResponse>
        {
            IsOk = true,
            Data = result.Value,
            StatusCode = StatusCodes.Status200OK,
            CodeText = "OK"
        });
    }

    private static async Task<Results<Created<ApiResponse<CategoryResponse>>, Conflict<ApiResponse<CategoryResponse>>>> CreateAsync(
        CreateCategoryRequest request,
        ClaimsPrincipal user,
        ICategoryService categoryService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await categoryService.CreateAsync(userId, request, ct);

        if (result.IsFailure)
            return TypedResults.Conflict(new ApiResponse<CategoryResponse>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Category Creation Failed",
                    Status = StatusCodes.Status409Conflict,
                    Detail = result.Error?.Description,
                },
                StatusCode = StatusCodes.Status409Conflict,
                CodeText = "CONFLICT"
            });

        return TypedResults.Created($"/api/categories/{result.Value!.Id}", new ApiResponse<CategoryResponse>
        {
            IsOk = true,
            Data = result.Value,
            StatusCode = StatusCodes.Status201Created,
            CodeText = "CREATED"
        });
    }

    private static async Task<Results<Ok<ApiResponse<CategoryResponse>>, NotFound<ApiResponse<CategoryResponse>>>> UpdateAsync(
        Guid id,
        UpdateCategoryRequest request,
        ClaimsPrincipal user,
        ICategoryService categoryService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await categoryService.UpdateAsync(userId, id, request, ct);

        if (result.IsFailure)
            return TypedResults.NotFound(new ApiResponse<CategoryResponse>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Category Update Failed",
                    Status = StatusCodes.Status404NotFound,
                    Detail = result.Error?.Description,
                },
                StatusCode = StatusCodes.Status404NotFound,
                CodeText = "NOT_FOUND"
            });

        return TypedResults.Ok(new ApiResponse<CategoryResponse>
        {
            IsOk = true,
            Data = result.Value,
            StatusCode = StatusCodes.Status200OK,
            CodeText = "OK"
        });
    }

    private static async Task<Results<NoContent, NotFound<ApiResponse<object>>>> DeleteAsync(
        Guid id,
        ClaimsPrincipal user,
        ICategoryService categoryService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await categoryService.DeleteAsync(userId, id, ct);

        if (result.IsFailure)
            return TypedResults.NotFound(new ApiResponse<object>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Category Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = result.Error?.Description,
                },
                StatusCode = StatusCodes.Status404NotFound,
                CodeText = "NOT_FOUND"
            });

        return TypedResults.NoContent();
    }
}