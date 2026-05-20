using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Personal.FinanceTracker.Shared.Extensions;
using Personal.FinanceTracker.Shared.Filters;
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
    private static async Task<Results<Ok<AuthResponse>, Conflict<string>>> RegisterAsync(
        RegisterRequest request,
        IUserService userService,
        CancellationToken ct)
    {
        var result = await userService.RegisterAsync(request, ct);
        if (result is null)
            return TypedResults.Conflict("An account with this email address already exists.");
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<AuthResponse>, UnauthorizedHttpResult>> LoginAsync(
        LoginRequest request,
        IUserService userService,
        CancellationToken ct)
    {
        var result = await userService.LoginAsync(request, ct);
        if (result is null)
            return TypedResults.Unauthorized();
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<AuthResponse>, UnauthorizedHttpResult>> RefreshAsync(
        RefreshTokenRequest request,
        IUserService userService,
        CancellationToken ct)
    {
        var result = await userService.RefreshTokenAsync(request.RefreshToken, ct);
        if (result is null)
            return TypedResults.Unauthorized();
        return TypedResults.Ok(result);
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult>> RevokeAsync(
        RefreshTokenRequest request,
        IUserService userService,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken ct)
    {
        var userId = claimsPrincipal.GetUserId();
        var success = await userService.RevokeTokenAsync(userId, request.RefreshToken, ct);
        if (!success)
            return TypedResults.Unauthorized();
        return TypedResults.NoContent();
    }
}