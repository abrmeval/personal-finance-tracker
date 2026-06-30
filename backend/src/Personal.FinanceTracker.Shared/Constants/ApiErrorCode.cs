namespace Personal.FinanceTracker.Shared.Constants;

/// <summary>
/// Defines standardized error codes for API responses across the application.
/// These codes provide a consistent way to identify specific error conditions, making it easier for clients to handle errors programmatically and for developers to maintain and troubleshoot the application. Each code corresponds to a common error scenario, such as resource conflicts, authentication failures, or invalid tokens.
/// </summary>
public static class ApiErrorCode
{
    public const string ResourceAlreadyExists = "RESOURCE_ALREADY_EXISTS";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string InvalidToken = "INVALID_TOKEN";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
    public const string TransactionNotFound = "TRANSACTION_NOT_FOUND";
    public const string DuplicateCategoryName = "DUPLICATE_CATEGORY_NAME";
}