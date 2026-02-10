using WdAlarm.Core.Enums;

namespace WdAlarm.Api.Models.Responses;

/// <summary>
/// Response model for alarm information
/// </summary>
public class AlarmResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public AlarmDelayType DelayType { get; set; }
    public TimeSpan? TimeoutDuration { get; set; }
    public string? CronExpression { get; set; }
    
    public bool AllowPingNoTimerReset { get; set; }
    public VerificationMethodType? VerificationMethod { get; set; }
    
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    public bool HasActiveCycle { get; set; }
    public DateTimeOffset? NextAlarmPoint { get; set; }
}
