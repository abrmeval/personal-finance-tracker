namespace Personal.FinanceTracker.Users.Application.DTOs.Responses;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName);