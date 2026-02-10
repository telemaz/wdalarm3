using WdAlarm.Core.Entities;

namespace WdAlarm.Core.Interfaces;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate access token for user
    /// </summary>
    /// <param name="user">User to generate token for</param>
    /// <returns>JWT access token</returns>
    string GenerateAccessToken(ApplicationUser user);

    /// <summary>
    /// Generate refresh token
    /// </summary>
    /// <returns>Refresh token string</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Get principal from expired token (for refresh)
    /// </summary>
    /// <param name="token">Expired access token</param>
    /// <returns>Claims principal from token</returns>
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
