using WdAlarm.Core.Enums;

namespace WdAlarm.Core.Entities;

public class AlarmCycle
{
    public Guid Id { get; set; }
    public Guid AlarmId { get; set; }
    
    // Cycle timing
    public DateTimeOffset AlarmPointTime { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? CompletedByPingId { get; set; }
    
    // Cycle status
    public AlarmCycleStatus Status { get; set; } = AlarmCycleStatus.Active;
    
    // Navigation properties
    public virtual Alarm? Alarm { get; set; }
    public virtual Ping? CompletedByPing { get; set; }
    public virtual ICollection<ActionExecution> ActionExecutions { get; set; } = new List<ActionExecution>();
}
