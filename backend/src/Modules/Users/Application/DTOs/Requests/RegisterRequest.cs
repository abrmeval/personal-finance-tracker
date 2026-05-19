namespace Personal.FinanceTracker.Users.Application.DTOs.Requests;
public sealed record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName);