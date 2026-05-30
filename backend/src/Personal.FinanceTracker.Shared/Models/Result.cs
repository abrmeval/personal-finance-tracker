namespace Personal.FinanceTracker.Shared.Models;

/// <summary>
/// Represents the result of an operation, which can either be a success with a value of type T, or a failure with an error message.
/// </summary>
/// <typeparam name="T"></typeparam>
public class Result<T>
{
    public bool IsSuccess { get; init; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; init; }
    public ErrorResult? Error { get; init; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value, Error = ErrorResult.None };
    public static Result<T> Failure(ErrorResult error) => new() { IsSuccess = false, Error = error };
}

/// <summary>
/// Represents an error result, containing a code and a description of the error. This can be used to provide more detailed information about why an operation failed.
/// </summary>
/// <param name="Code"></param>
/// <param name="Description"></param>
public sealed record ErrorResult(string Code, string Description)
{
    public static readonly ErrorResult None = new(string.Empty, string.Empty);
}