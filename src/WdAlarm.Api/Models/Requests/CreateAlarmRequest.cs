using System.ComponentModel.DataAnnotations;
using WdAlarm.Core.Enums;

namespace WdAlarm.Api.Models.Requests;

/// <summary>
/// Request model for creating a new alarm
/// </summary>
public class CreateAlarmRequest
{
    /// <summary>
    /// Name of the alarm
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Delay type: Timeout or Schedule
    /// </summary>
    [Required]
    public AlarmDelayType DelayType { get; set; }

    /// <summary>
    /// Timeout duration (required if DelayType is Timeout)
    /// Format: "HH:MM:SS" or ISO 8601 duration
    /// </summary>
    public TimeSpan? TimeoutDuration { get; set; }

    /// <summary>
    /// Cron expression (required if DelayType is Schedule)
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Allow pings without resetting the timer
    /// </summary>
    public bool AllowPingNoTimerReset { get; set; }

    /// <summary>
    /// Verification method type
    /// </summary>
    public VerificationMethodType? VerificationMethod { get; set; }

    /// <summary>
    /// Verification configuration (JSON string)
    /// </summary>
    public string? VerificationConfig { get; set; }
}
