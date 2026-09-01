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

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transactions")
            .WithTags("Transactions")
            .RequireAuthorization();

        group.MapGet("/", GetAllAsync)
            .WithName("GetTransactions")
            .WithDescription("Get a paginated, filtered list of transactions for the authenticated user.");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetTransactionById")
            .WithDescription("Get a single transaction by ID.");

        group.MapPost("/", CreateAsync)
            .WithName("CreateTransaction")
            .WithDescription("Create a new transaction.")
            .AddEndpointFilter<ValidationFilter<CreateTransactionRequest>>();

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateTransaction")
            .WithDescription("Update an existing transaction.")
            .AddEndpointFilter<ValidationFilter<UpdateTransactionRequest>>();

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteTransaction")
            .WithDescription("Delete a transaction.");

        return app;
    }

    private static async Task<Ok<ApiResponse<PagedResult<TransactionResponse>>>> GetAllAsync(
        ClaimsPrincipal user,
        [AsParameters] TransactionQueryParams queryParams,
        ITransactionService transactionService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await transactionService.GetAllAsync(userId, queryParams, ct);

        return TypedResults.Ok(new ApiResponse<PagedResult<TransactionResponse>>
        {
            IsOk = true,
            Data = result.Value,
            StatusCode = StatusCodes.Status200OK,
            CodeText = "OK"
        });
    }

    private static async Task<Results<Ok<ApiResponse<TransactionResponse>>, NotFound<ApiResponse<TransactionResponse>>>> GetByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        ITransactionService transactionService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await transactionService.GetByIdAsync(userId, id, ct);

        if (result.IsFailure)
            return TypedResults.NotFound(new ApiResponse<TransactionResponse>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Transaction Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = result.Error?.Description,
                },
                StatusCode = StatusCodes.Status404NotFound,
                CodeText = "NOT_FOUND"
            });

        return TypedResults.Ok(new ApiResponse<TransactionResponse>
        {
            IsOk = true,
            Data = result.Value,
            StatusCode = StatusCodes.Status200OK,
            CodeText = "OK"
        });
    }

    private static async Task<Results<Created<ApiResponse<TransactionResponse>>, BadRequest<ApiResponse<TransactionResponse>>>> CreateAsync(
        CreateTransactionRequest request,
        ClaimsPrincipal user,
        ITransactionService transactionService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await transactionService.CreateAsync(userId, request, ct);

        if (result.IsFailure)
            return TypedResults.BadRequest(new ApiResponse<TransactionResponse>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Transaction Creation Failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = result.Error?.Description,
                },
                StatusCode = StatusCodes.Status400BadRequest,
                CodeText = "BAD_REQUEST"
            });

        return TypedResults.Created($"/api/transactions/{result.Value!.Id}", new ApiResponse<TransactionResponse>
        {
            IsOk = true,
            Data = result.Value,
            StatusCode = StatusCodes.Status201Created,
            CodeText = "CREATED"
        });
    }

    private static async Task<Results<Ok<ApiResponse<TransactionResponse>>, NotFound<ApiResponse<TransactionResponse>>>> UpdateAsync(
        Guid id,
        UpdateTransactionRequest request,
        ClaimsPrincipal user,
        ITransactionService transactionService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await transactionService.UpdateAsync(userId, id, request, ct);

        if (result.IsFailure)
            return TypedResults.NotFound(new ApiResponse<TransactionResponse>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Transaction Update Failed",
                    Status = StatusCodes.Status404NotFound,
                    Detail = result.Error?.Description,
                },
                StatusCode = StatusCodes.Status404NotFound,
                CodeText = "NOT_FOUND"
            });

        return TypedResults.Ok(new ApiResponse<TransactionResponse>
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
        ITransactionService transactionService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await transactionService.DeleteAsync(userId, id, ct);

        if (result.IsFailure)
            return TypedResults.NotFound(new ApiResponse<object>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Transaction Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = result.Error?.Description,
                },
                StatusCode = StatusCodes.Status404NotFound,
                CodeText = "NOT_FOUND"
            });

        return TypedResults.NoContent();
    }
}