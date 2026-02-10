using Cronos;
using Microsoft.Extensions.Logging;
using WdAlarm.Core.Entities;
using WdAlarm.Core.Enums;
using WdAlarm.Core.Interfaces;

namespace WdAlarm.Infrastructure.Services;

public class AlarmCycleService : IAlarmCycleService
{
    private readonly IAlarmCycleRepository _cycleRepository;
    private readonly IActionExecutionRepository _actionExecutionRepository;
    private readonly ILogger<AlarmCycleService> _logger;

    public AlarmCycleService(
        IAlarmCycleRepository cycleRepository,
        IActionExecutionRepository actionExecutionRepository,
        ILogger<AlarmCycleService> logger)
    {
        _cycleRepository = cycleRepository;
        _actionExecutionRepository = actionExecutionRepository;
        _logger = logger;
    }

    public async Task<AlarmCycle> CreateCycleAsync(Alarm alarm, Ping? lastPing = null, CancellationToken cancellationToken = default)
    {
        var lastPingTime = lastPing?.ReceivedAt ?? DateTimeOffset.UtcNow;
        var alarmPointTime = CalculateAlarmPoint(alarm, lastPingTime);

        var cycle = new AlarmCycle
        {
            Id = Guid.NewGuid(),
            AlarmId = alarm.Id,
            AlarmPointTime = alarmPointTime,
            StartedAt = DateTimeOffset.UtcNow,
            Status = AlarmCycleStatus.Active
        };

        await _cycleRepository.AddAsync(cycle, cancellationToken);

        // Create action executions for delay actions
        var actionExecutions = alarm.DelayActions
            .Select(action => new ActionExecution
            {
                Id = Guid.NewGuid(),
                AlarmDelayActionId = action.Id,
                AlarmCycleId = cycle.Id,
                ActionType = action.ActionType,
                ScheduledTime = alarmPointTime.AddMilliseconds(action.OffsetMilliseconds),
                Status = ActionExecutionStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            })
            .OrderBy(ae => ae.ScheduledTime)
            .ToList();

        if (actionExecutions.Any())
        {
            await _actionExecutionRepository.AddRangeAsync(actionExecutions, cancellationToken);
        }

        _logger.LogInformation(
            "Created alarm cycle {CycleId} for alarm {AlarmId} with alarm point at {AlarmPointTime}",
            cycle.Id, alarm.Id, alarmPointTime);

        return cycle;
    }

    public async Task CompleteCycleAsync(Guid cycleId, Guid pingId, CancellationToken cancellationToken = default)
    {
        var cycle = await _cycleRepository.GetByIdAsync(cycleId, cancellationToken);
        if (cycle == null)
        {
            _logger.LogWarning("Cycle {CycleId} not found for completion", cycleId);
            return;
        }

        cycle.Status = AlarmCycleStatus.Completed;
        cycle.CompletedAt = DateTimeOffset.UtcNow;
        cycle.CompletedByPingId = pingId;

        await _cycleRepository.UpdateAsync(cycle, cancellationToken);

        // Cancel pending actions
        var pendingActions = await _actionExecutionRepository.GetPendingActionsAsync(DateTimeOffset.MaxValue, cancellationToken);
        var cyclePendingActions = pendingActions.Where(ae => ae.AlarmCycleId == cycleId).ToList();

        foreach (var action in cyclePendingActions)
        {
            action.Status = ActionExecutionStatus.Cancelled;
        }

        if (cyclePendingActions.Any())
        {
            await _actionExecutionRepository.UpdateRangeAsync(cyclePendingActions, cancellationToken);
        }

        _logger.LogInformation(
            "Completed cycle {CycleId}, cancelled {Count} pending actions",
            cycleId, cyclePendingActions.Count);
    }

    public async Task CancelCycleAsync(Guid cycleId, string reason, CancellationToken cancellationToken = default)
    {
        var cycle = await _cycleRepository.GetByIdAsync(cycleId, cancellationToken);
        if (cycle == null)
        {
            _logger.LogWarning("Cycle {CycleId} not found for cancellation", cycleId);
            return;
        }

        cycle.Status = AlarmCycleStatus.Cancelled;
        cycle.CompletedAt = DateTimeOffset.UtcNow;

        await _cycleRepository.UpdateAsync(cycle, cancellationToken);

        // Cancel pending actions
        var pendingActions = await _actionExecutionRepository.GetPendingActionsAsync(DateTimeOffset.MaxValue, cancellationToken);
        var cyclePendingActions = pendingActions.Where(ae => ae.AlarmCycleId == cycleId).ToList();

        foreach (var action in cyclePendingActions)
        {
            action.Status = ActionExecutionStatus.Cancelled;
        }

        if (cyclePendingActions.Any())
        {
            await _actionExecutionRepository.UpdateRangeAsync(cyclePendingActions, cancellationToken);
        }

        _logger.LogInformation(
            "Cancelled cycle {CycleId}, reason: {Reason}, cancelled {Count} pending actions",
            cycleId, reason, cyclePendingActions.Count);
    }

    public DateTimeOffset CalculateAlarmPoint(Alarm alarm, DateTimeOffset? lastPingTime)
    {
        var baseTime = lastPingTime ?? DateTimeOffset.UtcNow;

        return alarm.DelayType switch
        {
            AlarmDelayType.Timeout => baseTime.Add(alarm.TimeoutDuration ?? TimeSpan.FromHours(1)),
            AlarmDelayType.Schedule => CalculateNextScheduleOccurrence(alarm.CronExpression!, baseTime),
            _ => throw new ArgumentException($"Unknown delay type: {alarm.DelayType}")
        };
    }

    private DateTimeOffset CalculateNextScheduleOccurrence(string cronExpression, DateTimeOffset after)
    {
        try
        {
            var expression = CronExpression.Parse(cronExpression);
            var next = expression.GetNextOccurrence(after, TimeZoneInfo.Utc);

            if (next == null)
            {
                throw new InvalidOperationException($"Could not calculate next occurrence for cron: {cronExpression}");
            }

            return next.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing cron expression: {CronExpression}", cronExpression);
            throw;
        }
    }
}
