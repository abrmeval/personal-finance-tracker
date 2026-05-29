using Microsoft.AspNetCore.Mvc;

namespace Personal.FinanceTracker.Shared.Models;

/// <summary>
/// Represents an error response from the API, containing details about the error and any relevant context.
/// </summary>
public sealed class ApiError : ProblemDetails
{
    public string? Context { get; init; }

    /// <summary>
    /// Optional dictionary to hold model validation errors, where the key is the name of the field and the value is an array of error messages related to that field.
    /// </summary>
    public Dictionary<string, string[]>? ModelErrors { get; init; }
}