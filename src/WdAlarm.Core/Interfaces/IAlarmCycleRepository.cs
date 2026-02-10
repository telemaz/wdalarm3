using WdAlarm.Core.Entities;

namespace WdAlarm.Core.Interfaces;

public interface IAlarmCycleRepository
{
    Task<AlarmCycle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AlarmCycle?> GetActiveCycleByAlarmIdAsync(Guid alarmId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AlarmCycle>> GetActiveCyclesAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AlarmCycle cycle, CancellationToken cancellationToken = default);
    Task UpdateAsync(AlarmCycle cycle, CancellationToken cancellationToken = default);
}
