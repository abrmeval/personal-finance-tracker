using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Personal.FinanceTracker.Shared.Exceptions;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Shared.Middleware;

/// <summary>
/// Middleware for handling exceptions globally in the application. It catches unhandled exceptions, logs them, and returns appropriate HTTP responses based on the type of exception. 
/// This ensures that clients receive consistent error responses and that server errors are properly logged for troubleshooting.
/// </summary>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="logger">The logger instance for logging exceptions.</param>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "The requested resource was not found."),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Not authorized to access this resource."),
            FluentValidation.ValidationException => (StatusCodes.Status400BadRequest, "The request is invalid."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred while processing the request.")
        };

        var apiError = new ApiError
        {
            Type = exception.GetType().Name,
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new ApiResponse<object>
        {
            IsOk = false,
            Error = apiError,
            StatusCode = statusCode,
            CodeText = title
        });
    }
}