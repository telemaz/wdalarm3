namespace WdAlarm.Core.Entities;

public class Ping
{
    public Guid Id { get; set; }
    public Guid AlarmId { get; set; }
    public Guid? AlarmCycleId { get; set; }
    
    // Ping data
    public DateTimeOffset ReceivedAt { get; set; }
    public string? Payload { get; set; }
    
    // Verification
    public string? VerificationProof { get; set; }
    public bool VerificationResult { get; set; }
    public string? VerificationError { get; set; }
    
    // Behavior
    public bool ResetTimerRequested { get; set; } = true;
    public bool TimerWasReset { get; set; }
    
    // Metadata
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    
    // Navigation properties
    public virtual Alarm? Alarm { get; set; }
    public virtual AlarmCycle? AlarmCycle { get; set; }
    public virtual ICollection<ActionExecution> ActionExecutions { get; set; } = new List<ActionExecution>();
}
