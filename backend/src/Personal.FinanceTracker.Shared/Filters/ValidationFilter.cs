using FluentValidation;
using Microsoft.AspNetCore.Http;
using Personal.FinanceTracker.Shared.Models;

namespace Personal.FinanceTracker.Shared.Filters;

/// <summary>
/// Endpoint filter for validating incoming requests using FluentValidation. 
/// It checks the request body against the specified validator and returns a 400 Bad Request response with validation errors if the validation fails.
/// If the validation succeeds, it allows the request to proceed to the next filter or endpoint handler.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="validator"></param>
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();

        if (argument is null)
            return TypedResults.BadRequest(new ApiResponse<object>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Validation Failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "Request body is missing or invalid.",
                    Instance = context.HttpContext.Request.Path,
                },
                StatusCode = StatusCodes.Status400BadRequest,
                CodeText = "Bad Request"
            });

        var result = await validator.ValidateAsync(argument);

        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return TypedResults.BadRequest(new ApiResponse<object>
            {
                IsOk = false,
                Error = new ApiError
                {
                    Title = "Validation Failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "One or more validation errors occurred.",
                    Instance = context.HttpContext.Request.Path,
                    ModelErrors = errors
                },
                StatusCode = StatusCodes.Status400BadRequest,
                CodeText = "Bad Request"
            });
        }

        return await next(context);
    }
}