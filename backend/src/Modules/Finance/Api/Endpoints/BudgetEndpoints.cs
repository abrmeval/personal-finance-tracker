using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Personal.FinanceTracker.Finance.Application.DTOs.Requests;
using Personal.FinanceTracker.Finance.Application.DTOs.Responses;
using Personal.FinanceTracker.Finance.Application.Interfaces;
using Personal.FinanceTracker.Shared.Constants;
using Personal.FinanceTracker.Shared.Extensions;
using Personal.FinanceTracker.Shared.Filters;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Finance.Api.Endpoints;

public static class BudgetEndpoints
{
    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/budgets")
            .WithTags("Budgets")
            .RequireAuthorization();

        group.MapGet("/", GetAllAsync)
            .WithName("GetBudgets")
            .WithDescription("Get all budgets for the authenticated user, including current period spending.");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetBudgetById")
            .WithDescription("Get a single budget with current period spending.");

        group.MapPost("/", CreateAsync)
            .WithName("CreateBudget")
            .WithDescription("Create a new budget for a category.")
            .AddEndpointFilter<ValidationFilter<CreateBudgetRequest>>();

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateBudget")
            .WithDescription("Update an existing budget's category, name, limit, or period.")
            .AddEndpointFilter<ValidationFilter<UpdateBudgetRequest>>();

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteBudget")
            .WithDescription("Soft-delete a budget. It is excluded from lists and duplicate checks.");

        return app;
    }

    private static async Task<Ok<ApiResponse<IReadOnlyList<BudgetWithSpendingResponse>>>> GetAllAsync(
        ClaimsPrincipal user,
        IBudgetService budgetService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await budgetService.GetAllAsync(userId, ct);

        return TypedResults.Ok(new ApiResponse<IReadOnlyList<BudgetWithSpendingResponse>>
        {
            IsOk = true,
            Data = result.Value,
            StatusCode = StatusCodes.Status200OK,
            CodeText = "OK"
        });
    }

    private static async Task<Results<Ok<ApiResponse<BudgetWithSpendingResponse>>, NotFound<ApiResponse<BudgetWithSpendingResponse>>>> GetByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        IBudgetService budgetService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await budgetService.GetByIdAsync(userId, id, ct);

        if (result.IsFailure)
            return TypedResults.NotFound(new ApiResponse<BudgetWithSpendingResponse>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Budget Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = result.Error?.Description,
                },
                StatusCode = StatusCodes.Status404NotFound,
                CodeText = "NOT_FOUND"
            });

        return TypedResults.Ok(new ApiResponse<BudgetWithSpendingResponse>
        {
            IsOk = true,
            Data = result.Value,
            StatusCode = StatusCodes.Status200OK,
            CodeText = "OK"
        });
    }

    private static async Task<Results<Created<ApiResponse<BudgetWithSpendingResponse>>, BadRequest<ApiResponse<BudgetWithSpendingResponse>>, Conflict<ApiResponse<BudgetWithSpendingResponse>>>> CreateAsync(
        CreateBudgetRequest request,
        ClaimsPrincipal user,
        IBudgetService budgetService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await budgetService.CreateAsync(userId, request, ct);

        if (result.IsFailure)
        {
            if (result.Error?.Code == ApiErrorCode.CategoryNotFound)
                return TypedResults.BadRequest(new ApiResponse<BudgetWithSpendingResponse>
                {
                    IsOk = false,
                    Error = new ApiError
                    {
                        Title = "Budget Creation Failed",
                        Status = StatusCodes.Status400BadRequest,
                        Detail = result.Error?.Description,
                    },
                    StatusCode = StatusCodes.Status400BadRequest,
                    CodeText = "BAD_REQUEST"
                });

            return TypedResults.Conflict(new ApiResponse<BudgetWithSpendingResponse>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Budget Creation Failed",
                    Status = StatusCodes.Status409Conflict,
                    Detail = result.Error?.Description,
                },
                StatusCode = StatusCodes.Status409Conflict,
                CodeText = "CONFLICT"
            });
        }

        return TypedResults.Created($"/api/budgets/{result.Value!.Id}", new ApiResponse<BudgetWithSpendingResponse>
        {
            IsOk = true,
            Data = result.Value,
            StatusCode = StatusCodes.Status201Created,
            CodeText = "CREATED"
        });
    }

    private static async Task<Results<Ok<ApiResponse<BudgetWithSpendingResponse>>, BadRequest<ApiResponse<BudgetWithSpendingResponse>>, Conflict<ApiResponse<BudgetWithSpendingResponse>>, NotFound<ApiResponse<BudgetWithSpendingResponse>>>> UpdateAsync(
        Guid id,
        UpdateBudgetRequest request,
        ClaimsPrincipal user,
        IBudgetService budgetService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await budgetService.UpdateAsync(userId, id, request, ct);

        if (result.IsFailure)
        {
            if (result.Error?.Code == ApiErrorCode.CategoryNotFound)
                return TypedResults.BadRequest(new ApiResponse<BudgetWithSpendingResponse>
                {
                    IsOk = false,
                    Error = new ApiError
                    {
                        Title = "Budget Update Failed",
                        Status = StatusCodes.Status400BadRequest,
                        Detail = result.Error?.Description,
                    },
                    StatusCode = StatusCodes.Status400BadRequest,
                    CodeText = "BAD_REQUEST"
                });

            if (result.Error?.Code == ApiErrorCode.DuplicateBudgetCategory)
                return TypedResults.Conflict(new ApiResponse<BudgetWithSpendingResponse>
                {
                    IsOk = false,
                    Error = new ApiError
                    {
                        Title = "Budget Update Failed",
                        Status = StatusCodes.Status409Conflict,
                        Detail = result.Error?.Description,
                    },
                    StatusCode = StatusCodes.Status409Conflict,
                    CodeText = "CONFLICT"
                });

            return TypedResults.NotFound(new ApiResponse<BudgetWithSpendingResponse>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Budget Update Failed",
                    Status = StatusCodes.Status404NotFound,
                    Detail = result.Error?.Description,
                },
                StatusCode = StatusCodes.Status404NotFound,
                CodeText = "NOT_FOUND"
            });
        }

        return TypedResults.Ok(new ApiResponse<BudgetWithSpendingResponse>
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
        IBudgetService budgetService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await budgetService.DeleteAsync(userId, id, ct);

        if (result.IsFailure)
            return TypedResults.NotFound(new ApiResponse<object>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Budget Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = result.Error?.Description,
                },
                StatusCode = StatusCodes.Status404NotFound,
                CodeText = "NOT_FOUND"
            });

        return TypedResults.NoContent();
    }
}
