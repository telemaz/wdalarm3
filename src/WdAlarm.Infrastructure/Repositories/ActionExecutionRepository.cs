using Microsoft.EntityFrameworkCore;
using WdAlarm.Core.Entities;
using WdAlarm.Core.Enums;
using WdAlarm.Core.Interfaces;
using WdAlarm.Infrastructure.Data;

namespace WdAlarm.Infrastructure.Repositories;

/// <summary>
/// Repository for managing ActionExecution entities
/// </summary>
public class ActionExecutionRepository : IActionExecutionRepository
{
    private readonly ApplicationDbContext context;

    public ActionExecutionRepository(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<IEnumerable<ActionExecution>> GetPendingActionsAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        return await context.ActionExecutions
            .Include(e => e.AlarmCycle)
                .ThenInclude(c => c!.Alarm)
            .Where(e => e.Status == ActionExecutionStatus.Pending && e.ScheduledTime <= now)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ActionExecution execution, CancellationToken cancellationToken = default)
    {
        await context.ActionExecutions.AddAsync(execution, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<ActionExecution> executions, CancellationToken cancellationToken = default)
    {
        await context.ActionExecutions.AddRangeAsync(executions, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ActionExecution execution, CancellationToken cancellationToken = default)
    {
        context.ActionExecutions.Update(execution);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(IEnumerable<ActionExecution> executions, CancellationToken cancellationToken = default)
    {
        context.ActionExecutions.UpdateRange(executions);
        await context.SaveChangesAsync(cancellationToken);
    }
}
