namespace Personal.FinanceTracker.Shared.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found in the system. This can be used for entities like transactions, accounts, etc. to indicate that the specified resource does not exist.
/// </summary>
/// <param name="resourceName">The name of the resource that was not found.</param>
/// <param name="key">The key or identifier of the resource that was not found.</param>
public sealed class NotFoundException(string resourceName, object key)
    : Exception($"{resourceName} with key '{key}' was not found.");