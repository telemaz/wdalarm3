using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WdAlarm.Core.Entities;
using WdAlarm.Core.Enums;
using WdAlarm.Core.Interfaces;

namespace WdAlarm.Infrastructure.Workers;

public class AlarmCycleManagerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AlarmCycleManagerWorker> _logger;

    public AlarmCycleManagerWorker(
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
                
                // Create cycles for alarms without active cycles
                var alarmRepo = scope.ServiceProvider.GetRequiredService<IAlarmRepository>();
                var cycleService = scope.ServiceProvider.GetRequiredService<IAlarmCycleService>();
                
                var alarmsWithoutCycles = await alarmRepo.GetWithoutActiveCycleAsync(stoppingToken);
                
                foreach (var alarm in alarmsWithoutCycles)
                {
                    try
                    {
                        await cycleService.CreateCycleAsync(alarm, null, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating cycle for alarm {AlarmId}", alarm.Id);
                    }
                }
                
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AlarmCycleManagerWorker");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        
        _logger.LogInformation("AlarmCycleManagerWorker stopping");
    }
}
