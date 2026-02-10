namespace WdAlarm.Api.Models.Responses;

/// <summary>
/// Response model for user registration
/// </summary>
public class RegisterResponse
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User email
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Success message
    /// </summary>
    public string Message { get; set; } = "Registration successful";
}
