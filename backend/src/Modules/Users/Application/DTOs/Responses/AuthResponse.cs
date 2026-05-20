namespace Personal.FinanceTracker.Users.Application.DTOs.Responses;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserResponse User);