using Personal.FinanceTracker.Users.Application.Interfaces;

namespace Personal.FinanceTracker.Users.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for JWT (JSON Web Token) authentication. These settings are typically loaded from a configuration file (e.g., appsettings.json) and used to configure the JWT authentication middleware in the application.
/// </summary>
public class JwtSettings : IJwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// A secret key used to sign the JWT. This should be a long, random string that is kept secure and not hard-coded in the source code.
    /// </summary>
    public string? SecretKey { get; init; }

    /// <summary>
    /// The issuer of the JWT. This is typically the URL of the authentication server or service that issues the token.
    /// </summary>
    public string? Issuer { get; init; }

    /// <summary>
    /// The intended audience for the JWT. This is typically the URL of the API or service that will consume the token. 
    /// It can be used to validate that the token is being used by the correct recipient.
    /// </summary>
    public string? Audience { get; init; }

    /// <summary>
    /// Number of minutes before the access token expires. Typically set to a short duration, such as 15 or 30 minutes.
    /// </summary>
    public int ExpiryMinutes { get; init; }

    /// <summary>
    /// Number of days before a refresh token expires. Typically set to a longer duration than the access token, such as 7 or 30 days.
    /// </summary>
    public int RefreshTokenExpiryDays { get; init; }
}