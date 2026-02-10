using WdAlarm.Core.Entities;

namespace WdAlarm.Core.Interfaces;

public interface IActionExecutionRepository
{
    Task<IEnumerable<ActionExecution>> GetPendingActionsAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
    Task AddAsync(ActionExecution execution, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<ActionExecution> executions, CancellationToken cancellationToken = default);
    Task UpdateAsync(ActionExecution execution, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<ActionExecution> executions, CancellationToken cancellationToken = default);
}
