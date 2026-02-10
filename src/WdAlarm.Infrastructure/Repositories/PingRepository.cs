using Microsoft.EntityFrameworkCore;
using WdAlarm.Core.Entities;
using WdAlarm.Core.Interfaces;
using WdAlarm.Infrastructure.Data;

namespace WdAlarm.Infrastructure.Repositories;

public class PingRepository : IPingRepository
{
    private readonly ApplicationDbContext context;

    public PingRepository(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<Ping?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Pings
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Ping>> GetByAlarmIdAsync(Guid alarmId, CancellationToken cancellationToken = default)
    {
        return await context.Pings
            .Where(p => p.AlarmId == alarmId)
            .OrderByDescending(p => p.ReceivedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Ping> AddAsync(Ping ping, CancellationToken cancellationToken = default)
    {
        context.Pings.Add(ping);
        await context.SaveChangesAsync(cancellationToken);
        return ping;
    }

    public async Task DeleteOlderThanAsync(DateTimeOffset cutoffDate, CancellationToken cancellationToken = default)
    {
        var oldPings = await context.Pings
            .Where(p => p.ReceivedAt < cutoffDate)
            .ToListAsync(cancellationToken);

        if (oldPings.Any())
        {
            context.Pings.RemoveRange(oldPings);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
