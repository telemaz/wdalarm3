using Microsoft.AspNetCore.Identity;

namespace WdAlarm.Core.Entities;

/// <summary>
/// Application user extending ASP.NET Core Identity user
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// User's first name
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// When the user account was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the user account was last updated
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Refresh token for JWT authentication
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// When the refresh token expires
    /// </summary>
    public DateTimeOffset? RefreshTokenExpiryTime { get; set; }

    /// <summary>
    /// Navigation property: Alarms owned by this user
    /// </summary>
    public ICollection<Alarm> Alarms { get; set; } = new List<Alarm>();
}
