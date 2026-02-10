using WdAlarm.Core.Enums;

namespace WdAlarm.Core.Entities;

public class AlarmPingAction
{
    public Guid Id { get; set; }
    public Guid AlarmId { get; set; }
    
    // Action configuration
    public ActionType ActionType { get; set; }
    public string ActionConfig { get; set; } = string.Empty; // JSON
    public int ExecutionOrder { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    // Navigation properties
    public virtual Alarm? Alarm { get; set; }
    public virtual ICollection<ActionExecution> Executions { get; set; } = new List<ActionExecution>();
}
