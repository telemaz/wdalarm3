using Microsoft.EntityFrameworkCore;
using WdAlarm.Core.Entities;
using WdAlarm.Core.Enums;
using WdAlarm.Core.Interfaces;
using WdAlarm.Infrastructure.Data;

namespace WdAlarm.Infrastructure.Repositories;

/// <summary>
/// Repository for managing AlarmCycle entities
/// </summary>
public class AlarmCycleRepository : IAlarmCycleRepository
{
    private readonly ApplicationDbContext context;

    public AlarmCycleRepository(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<AlarmCycle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.AlarmCycles
            .Include(c => c.Alarm)
            .Include(c => c.ActionExecutions)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<AlarmCycle?> GetActiveCycleByAlarmIdAsync(Guid alarmId, CancellationToken cancellationToken = default)
    {
        return await context.AlarmCycles
            .Include(c => c.Alarm)
            .Include(c => c.ActionExecutions)
            .FirstOrDefaultAsync(c => c.AlarmId == alarmId && c.Status == AlarmCycleStatus.Active, cancellationToken);
    }

    public async Task<IEnumerable<AlarmCycle>> GetActiveCyclesAsync(CancellationToken cancellationToken = default)
    {
        return await context.AlarmCycles
            .Include(c => c.Alarm)
                .ThenInclude(a => a.DelayActions)
            .Include(c => c.Alarm)
                .ThenInclude(a => a.PingActions)
            .Include(c => c.ActionExecutions)
            .Where(c => c.Status == AlarmCycleStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AlarmCycle cycle, CancellationToken cancellationToken = default)
    {
        await context.AlarmCycles.AddAsync(cycle, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AlarmCycle cycle, CancellationToken cancellationToken = default)
    {
        context.AlarmCycles.Update(cycle);
        await context.SaveChangesAsync(cancellationToken);
    }
}
