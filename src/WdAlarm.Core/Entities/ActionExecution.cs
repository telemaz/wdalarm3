using WdAlarm.Core.Enums;

namespace WdAlarm.Core.Entities;

public class ActionExecution
{
    public Guid Id { get; set; }
    
    // Source tracking
    public Guid? AlarmDelayActionId { get; set; }
    public Guid? AlarmPingActionId { get; set; }
    public Guid? AlarmCycleId { get; set; }
    public Guid? PingId { get; set; }
    
    // Execution details
    public ActionType ActionType { get; set; }
    public DateTimeOffset ScheduledTime { get; set; }
    public DateTimeOffset? ExecutedTime { get; set; }
    public ActionExecutionStatus Status { get; set; } = ActionExecutionStatus.Pending;
    
    // Retry tracking
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    // Navigation properties
    public virtual AlarmDelayAction? AlarmDelayAction { get; set; }
    public virtual AlarmPingAction? AlarmPingAction { get; set; }
    public virtual AlarmCycle? AlarmCycle { get; set; }
    public virtual Ping? Ping { get; set; }
}
