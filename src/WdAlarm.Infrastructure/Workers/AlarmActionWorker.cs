using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WdAlarm.Core.Entities;
using WdAlarm.Core.Enums;
using WdAlarm.Core.Interfaces;

namespace WdAlarm.Infrastructure.Workers;

public class AlarmActionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AlarmCycleManagerWorker> _logger;

    public AlarmActionWorker(
        IServiceProvider serviceProvider,
        ILogger<AlarmCycleManagerWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AlarmCycleManagerWorker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var actionExecutionRepo = scope.ServiceProvider.GetRequiredService<IActionExecutionRepository>();
                var actionExecutor = scope.ServiceProvider.GetRequiredService<IActionExecutor>();

                var now = DateTimeOffset.UtcNow;
                
                // Get pending actions that are due
                IEnumerable<ActionExecution> elapsedActions = (await actionExecutionRepo.GetPendingActionsAsync(now, stoppingToken)).Where(a => a.ScheduledTime <= DateTimeOffset.UtcNow);
                
                if (elapsedActions.Any())
                {
                    // Group by alarm to execute in parallel

                    
                    IEnumerable<IGrouping<Guid?, ActionExecution>> groupedByAlarm = elapsedActions.GroupBy(a => a.AlarmCycleId);
                    Parallel.ForEach(elapsedActions, a =>
                    {
                        _ = Task.Run(async () =>
                        {
                            using IServiceScope execScope = _serviceProvider.CreateScope();
                            IActionExecutor executor = execScope.ServiceProvider.GetRequiredService<IActionExecutor>();
                            IActionExecutionRepository repo = execScope.ServiceProvider.GetRequiredService<IActionExecutionRepository>();
                            
                            foreach (var action in elapsedActions)
                            {
                                await ExecuteActionAsync(action, executor, repo, stoppingToken);
                            }
                        }, stoppingToken);
                    });
                }
                
                await Task.Delay(TimeSpan.FromMilliseconds(10), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AlarmCycleManagerWorker");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        
        _logger.LogInformation("AlarmCycleManagerWorker stopping");
    }

    private async Task ExecuteActionAsync(
        ActionExecution action,
        IActionExecutor executor,
        IActionExecutionRepository repo,
        CancellationToken cancellationToken)
    {
        var maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(5);

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                action.Status = ActionExecutionStatus.Executing;
                await repo.UpdateAsync(action, cancellationToken);

                var result = await executor.ExecuteAsync(action, cancellationToken);

                if (result.IsSuccess)
                {
                    action.Status = ActionExecutionStatus.Completed;
                    action.ExecutedTime = DateTimeOffset.UtcNow;
                    action.ErrorMessage = null;
                    await repo.UpdateAsync(action, cancellationToken);

                    _logger.LogInformation(
                        "Successfully executed action {ActionId} (Attempt {Attempt})",
                        action.Id, attempt + 1);
                    return;
                }

                // Failure
                if (!result.IsRetriable || attempt >= maxRetries)
                {
                    action.Status = ActionExecutionStatus.Failed;
                    action.ErrorMessage = result.ErrorMessage;
                    action.RetryCount = attempt;
                    await repo.UpdateAsync(action, cancellationToken);

                    _logger.LogError(
                        "Action {ActionId} failed permanently: {Error}",
                        action.Id, result.ErrorMessage);
                    return;
                }

                // Retry
                action.RetryCount = attempt + 1;
                action.Status = ActionExecutionStatus.Pending;
                await repo.UpdateAsync(action, cancellationToken);

                _logger.LogWarning(
                    "Action {ActionId} failed (Attempt {Attempt}), retrying: {Error}",
                    action.Id, attempt + 1, result.ErrorMessage);

                await Task.Delay(retryDelay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception executing action {ActionId}", action.Id);

                if (attempt >= maxRetries)
                {
                    action.Status = ActionExecutionStatus.Failed;
                    action.ErrorMessage = ex.Message;
                    action.RetryCount = attempt;
                    await repo.UpdateAsync(action, cancellationToken);
                    return;
                }

                await Task.Delay(retryDelay, cancellationToken);
            }
        }
    }
}