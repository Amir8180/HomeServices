using System.Security.Claims;
using HomeServices.Identity.Application.Dtos;
using HomeServices.Identity.Domain.Entities;

namespace HomeServices.Identity.Application.Interfaces;

/// <summary>
/// JWT token generation and validation contract.
/// </summary>
public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateAccessToken(string token);
    DateTime GetAccessTokenExpiration();
}
