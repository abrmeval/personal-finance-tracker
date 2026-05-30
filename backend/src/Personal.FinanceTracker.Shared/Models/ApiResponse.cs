namespace Personal.FinanceTracker.Shared.Models;

/// <summary>
/// Represents a standardized response from the API, encapsulating the success status, data payload, error information, and HTTP status code. This class is used to ensure consistent API responses across all endpoints, making it easier for clients to handle responses and errors in a uniform way.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
public sealed class ApiResponse<T>
{
   public bool IsOk { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }
    public int StatusCode { get; init; }
    public string? CodeText { get; init; }
}