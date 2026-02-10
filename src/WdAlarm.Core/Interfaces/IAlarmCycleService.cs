using WdAlarm.Core.Entities;

namespace WdAlarm.Core.Interfaces;

public interface IAlarmCycleService
{
    Task<AlarmCycle> CreateCycleAsync(Alarm alarm, Ping? lastPing = null, CancellationToken cancellationToken = default);
    Task CompleteCycleAsync(Guid cycleId, Guid pingId, CancellationToken cancellationToken = default);
    Task CancelCycleAsync(Guid cycleId, string reason, CancellationToken cancellationToken = default);
    DateTimeOffset CalculateAlarmPoint(Alarm alarm, DateTimeOffset? lastPingTime);
}
