using WdAlarm.Core.DTOs;
using WdAlarm.Core.Entities;

namespace WdAlarm.Core.Interfaces;

public interface IActionExecutor
{
    Task<ActionExecutionResult> ExecuteAsync(ActionExecution actionExecution, CancellationToken cancellationToken = default);
}
