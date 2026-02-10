using WdAlarm.Core.Enums;

namespace WdAlarm.Core.Entities;

public class Alarm
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Delay configuration
    public AlarmDelayType DelayType { get; set; }
    public TimeSpan? TimeoutDuration { get; set; }
    public string? CronExpression { get; set; }
    
    // Ping behavior
    public bool AllowPingNoTimerReset { get; set; }
    
    // Verification configuration
    public VerificationMethodType? VerificationMethod { get; set; }
    public string? VerificationConfig { get; set; } // JSON
    
    // Metadata
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<AlarmDelayAction> DelayActions { get; set; } = new List<AlarmDelayAction>();
    public virtual ICollection<AlarmPingAction> PingActions { get; set; } = new List<AlarmPingAction>();
    public virtual ICollection<AlarmCycle> Cycles { get; set; } = new List<AlarmCycle>();
    public virtual ICollection<Ping> Pings { get; set; } = new List<Ping>();
}
