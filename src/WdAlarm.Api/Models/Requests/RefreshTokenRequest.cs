using System.ComponentModel.DataAnnotations;

namespace WdAlarm.Api.Models.Requests;

/// <summary>
/// Request model for refreshing access token
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Refresh token
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
