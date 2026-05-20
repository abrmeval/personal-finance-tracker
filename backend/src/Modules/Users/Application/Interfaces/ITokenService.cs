using System.Security.Claims;
using Personal.FinanceTracker.Users.Domain.Entities;
namespace Personal.FinanceTracker.Users.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}