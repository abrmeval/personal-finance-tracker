namespace Personal.FinanceTracker.Users.Application.Interfaces;

/// <summary>
/// Represents the JWT settings required for token generation and validation.
/// </summary>
public interface IJwtSettings
{
    /// <summary>
    /// A secret key used to sign the JWT. This should be a long, random string that is kept secure and not hard-coded in the source code.
    /// </summary>
    string? SecretKey { get; }

    /// <summary>
    /// The issuer of the JWT. This is typically the URL of the authentication server or service that issues the token.
    /// </summary>
    string? Issuer { get; }

    /// <summary>
    /// The intended audience for the JWT. This is typically the URL of the API or service that will consume the token. 
    /// It can be used to validate that the token is being used by the correct recipient.
    /// </summary>
    string? Audience { get; }

    /// <summary>
    /// Number of minutes before the access token expires. Typically set to a short duration, such as 15 or 30 minutes.
    /// </summary>
    int ExpiryMinutes { get; }

    /// <summary>
    /// Number of days before a refresh token expires. Typically set to a longer duration than the access token, such as 7 or 30 days.
    /// </summary>
    int RefreshTokenExpiryDays { get; }
}