using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Personal.FinanceTracker.Reporting.Application.DTOs.Requests;
using Personal.FinanceTracker.Reporting.Application.DTOs.Responses;
using Personal.FinanceTracker.Reporting.Application.Interfaces;
using Personal.FinanceTracker.Shared.Extensions;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Reporting.Api.Endpoints;

public static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
            .WithTags("Reports")
            .RequireAuthorization();

        group.MapGet("/dashboard/summary", GetSummaryAsync)
            .WithName("GetDashboardSummary")
            .WithDescription("Get total balance and current-month income/expense totals for the authenticated user.");

        group.MapGet("/dashboard/income-vs-expenses", GetIncomeVsExpensesAsync)
            .WithName("GetIncomeVsExpenses")
            .WithDescription("Get monthly income and expense totals for the last N months (default 6, max 24).");

        group.MapGet("/dashboard/category-breakdown", GetCategoryBreakdownAsync)
            .WithName("GetCategoryBreakdown")
            .WithDescription("Get expense totals grouped by category for a date range.");

        return app;
    }

    private static async Task<Ok<ApiResponse<DashboardSummaryResponse>>> GetSummaryAsync(
        ClaimsPrincipal user,
        IDashboardService dashboardService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await dashboardService.GetSummaryAsync(userId, ct);

        return TypedResults.Ok(new ApiResponse<DashboardSummaryResponse>
        {
            IsOk = true,
            Data = result.Value,
            StatusCode = StatusCodes.Status200OK,
            CodeText = "OK"
        });
    }

    private static async Task<Results<Ok<ApiResponse<IReadOnlyList<MonthlyTotalsResponse>>>, BadRequest<ApiResponse<IReadOnlyList<MonthlyTotalsResponse>>>>> GetIncomeVsExpensesAsync(
        ClaimsPrincipal user,
        [AsParameters] IncomeVsExpensesQueryParams queryParams,
        IDashboardService dashboardService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await dashboardService.GetIncomeVsExpensesAsync(userId, queryParams.Months, ct);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(new ApiResponse<IReadOnlyList<MonthlyTotalsResponse>>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Invalid Report Parameters",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = result.Error?.Description,
                },
                StatusCode = StatusCodes.Status400BadRequest,
                CodeText = "BAD_REQUEST"
            });
        }

        return TypedResults.Ok(new ApiResponse<IReadOnlyList<MonthlyTotalsResponse>>
        {
            IsOk = true,
            Data = result.Value,
            StatusCode = StatusCodes.Status200OK,
            CodeText = "OK"
        });
    }

    private static async Task<Results<Ok<ApiResponse<IReadOnlyList<CategoryBreakdownResponse>>>, BadRequest<ApiResponse<IReadOnlyList<CategoryBreakdownResponse>>>>> GetCategoryBreakdownAsync(
        ClaimsPrincipal user,
        [AsParameters] CategoryBreakdownQueryParams queryParams,
        IDashboardService dashboardService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        var result = await dashboardService.GetCategoryBreakdownAsync(userId, queryParams.StartDate, queryParams.EndDate, ct);

        if (result.IsFailure)
        {
            return TypedResults.BadRequest(new ApiResponse<IReadOnlyList<CategoryBreakdownResponse>>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Invalid Report Parameters",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = result.Error?.Description,
                },
                StatusCode = StatusCodes.Status400BadRequest,
                CodeText = "BAD_REQUEST"
            });
        }

        return TypedResults.Ok(new ApiResponse<IReadOnlyList<CategoryBreakdownResponse>>
        {
            IsOk = true,
            Data = result.Value,
            StatusCode = StatusCodes.Status200OK,
            CodeText = "OK"
        });
    }
}
