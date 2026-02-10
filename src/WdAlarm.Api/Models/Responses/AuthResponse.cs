namespace WdAlarm.Api.Models.Responses;

/// <summary>
/// Response model for authentication endpoints
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// JWT access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration in seconds
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Token type (always "Bearer")
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User email
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
