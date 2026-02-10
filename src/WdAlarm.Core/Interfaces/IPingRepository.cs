using WdAlarm.Core.Entities;

namespace WdAlarm.Core.Interfaces;

public interface IPingRepository
{
    Task<Ping?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Ping>> GetByAlarmIdAsync(Guid alarmId, CancellationToken cancellationToken = default);
    Task<Ping> AddAsync(Ping ping, CancellationToken cancellationToken = default);
    Task DeleteOlderThanAsync(DateTimeOffset cutoffDate, CancellationToken cancellationToken = default);
}
