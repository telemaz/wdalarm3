using Microsoft.EntityFrameworkCore;
using WdAlarm.Core.Entities;
using WdAlarm.Core.Interfaces;
using WdAlarm.Infrastructure.Data;

namespace WdAlarm.Infrastructure.Repositories;

/// <summary>
/// Repository for managing Alarm entities
/// </summary>
public class AlarmRepository : IAlarmRepository
{
    private readonly ApplicationDbContext context;

    public AlarmRepository(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<Alarm?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Alarms
            .Include(a => a.DelayActions)
            .Include(a => a.PingActions)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Alarm>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await context.Alarms
            .Where(a => a.IsActive)
            .Include(a => a.DelayActions)
            .Include(a => a.PingActions)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Alarm>> GetWithoutActiveCycleAsync(CancellationToken cancellationToken = default)
    {
        return await context.Alarms
            .Where(a => a.IsActive && !a.Cycles.Any(c => c.Status == Core.Enums.AlarmCycleStatus.Active))
            .Include(a => a.DelayActions)
            .Include(a => a.PingActions)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Alarm alarm, CancellationToken cancellationToken = default)
    {
        await context.Alarms.AddAsync(alarm, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Alarm alarm, CancellationToken cancellationToken = default)
    {
        context.Alarms.Update(alarm);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var alarm = await context.Alarms.FindAsync(new object[] { id }, cancellationToken);
        if (alarm != null)
        {
            context.Alarms.Remove(alarm);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
