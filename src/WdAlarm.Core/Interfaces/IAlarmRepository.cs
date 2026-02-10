using WdAlarm.Core.Entities;

namespace WdAlarm.Core.Interfaces;

public interface IAlarmRepository
{
    Task<Alarm?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Alarm>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Alarm>> GetWithoutActiveCycleAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Alarm alarm, CancellationToken cancellationToken = default);
    Task UpdateAsync(Alarm alarm, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
