using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Personal.FinanceTracker.Shared.Abstractions;
using Personal.FinanceTracker.Shared.Extensions;
using Personal.FinanceTracker.Shared.Filters;
using Personal.FinanceTracker.Shared.Models;
using Personal.FinanceTracker.Users.Application.DTOs.Requests;
using Personal.FinanceTracker.Users.Application.DTOs.Responses;
using Personal.FinanceTracker.Users.Application.Interfaces;
namespace Personal.FinanceTracker.Users.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // Grouping authentication-related endpoints under a common route prefix and tag for better organization and discoverability in API documentation.
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithDescription("Create a new user account and return tokens.")
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>();

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithDescription("Authenticate with email and password and return tokens.")
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();

        group.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshToken")
            .WithDescription("Exchange a valid refresh token for a new token pair.");

        group.MapPost("/revoke", RevokeAsync)
            .WithName("RevokeToken")
            .WithDescription("Revoke the current refresh token.")
            .RequireAuthorization();
        return app;
    }
    private static async Task<Results<Ok<ApiResponse<AuthResponse>>, Conflict<ApiResponse<AuthResponse>>>> RegisterAsync(
        RegisterRequest request,
        IUserService userService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await userService.RegisterAsync(request, ct);
        
        if (result is null)
            return TypedResults.Conflict(new ApiResponse<AuthResponse>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Registration Failed",
                    Status = StatusCodes.Status409Conflict,
                    Detail = "An account with this email address already exists.",
                    Instance = httpContext.Request.Path,
                },
                StatusCode =StatusCodes.Status409Conflict,
                CodeText = "Conflict"
            });

        return TypedResults.Ok(new ApiResponse<AuthResponse>
        {
            IsOk = true,
            Data = result,
            StatusCode = StatusCodes.Status200OK,
            CodeText = "OK"
        });
    }

    private static async Task<Results<Ok<ApiResponse<AuthResponse>>, JsonHttpResult<ApiResponse<AuthResponse>>>> LoginAsync(
        LoginRequest request,
        IUserService userService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await userService.LoginAsync(request, ct);

        if (result is null)
            return TypedResults.Json(new ApiResponse<AuthResponse>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Authentication Failed",
                    Status = StatusCodes.Status401Unauthorized,
                    Detail = "Invalid email or password.",
                    Instance = httpContext.Request.Path,
                },
                StatusCode = StatusCodes.Status401Unauthorized,
                CodeText = "Unauthorized"
            }, statusCode: StatusCodes.Status401Unauthorized);

        return TypedResults.Ok(new ApiResponse<AuthResponse>
        {
            IsOk = true,
            Data = result,
            StatusCode = StatusCodes.Status200OK,
            CodeText = "OK"
        });
    }

    private static async Task<Results<Ok<ApiResponse<AuthResponse>>, JsonHttpResult<ApiResponse<AuthResponse>>>> RefreshAsync(
        RefreshTokenRequest request,
        IUserService userService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await userService.RefreshTokenAsync(request.RefreshToken, ct);
        
        if (result is null)
            return TypedResults.Json(new ApiResponse<AuthResponse>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Refresh Token Failed",
                    Status = StatusCodes.Status401Unauthorized,
                    Detail = "Invalid refresh token or token has expired.",
                    Instance = httpContext.Request.Path,
                },
                StatusCode = StatusCodes.Status401Unauthorized,
                CodeText = "Unauthorized"
            }, statusCode: StatusCodes.Status401Unauthorized);

        return TypedResults.Ok(new ApiResponse<AuthResponse>
        {
            IsOk = true,
            Data = result,
            StatusCode = StatusCodes.Status200OK,
            CodeText = "OK"
        });
    }

    private static async Task<Results<NoContent, JsonHttpResult<ApiResponse<AuthResponse>>>> RevokeAsync(
        RefreshTokenRequest request,
        IUserService userService,
        HttpContext httpContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken ct)
    {
        var userId = claimsPrincipal.GetUserId();
        var success = await userService.RevokeTokenAsync(userId, request.RefreshToken, ct);

        if (!success)
            return TypedResults.Json(new ApiResponse<AuthResponse>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Revocation Failed",
                    Status = StatusCodes.Status401Unauthorized,
                    Detail = "Invalid refresh token or token does not belong to the user.",
                    Instance = httpContext.Request.Path,
                },
                StatusCode = StatusCodes.Status401Unauthorized,
                CodeText = "Unauthorized"
            }, statusCode: StatusCodes.Status401Unauthorized);

        return TypedResults.NoContent();
    }
}