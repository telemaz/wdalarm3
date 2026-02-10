using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WdAlarm.Core.Entities;
using WdAlarm.Core.Interfaces;

namespace WdAlarm.Api.Controllers;

/// <summary>
/// Controller for ping/check-in operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PingController : ControllerBase
{
    private readonly IAlarmRepository alarmRepository;
    private readonly IAlarmCycleRepository cycleRepository;
    private readonly IAlarmCycleService cycleService;
    private readonly IPingRepository pingRepository;
    private readonly ILogger<PingController> logger;

    public PingController(
        IAlarmRepository alarmRepository,
        IAlarmCycleRepository cycleRepository,
        IAlarmCycleService cycleService,
        IPingRepository pingRepository,
        ILogger<PingController> logger)
    {
        this.alarmRepository = alarmRepository;
        this.cycleRepository = cycleRepository;
        this.cycleService = cycleService;
        this.pingRepository = pingRepository;
        this.logger = logger;
    }

    /// <summary>
    /// Send a ping/check-in for an alarm
    /// </summary>
    /// <param name="alarmId">Alarm ID to ping</param>
    [HttpPost("{alarmId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendPing(Guid alarmId)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var alarm = await alarmRepository.GetByIdAsync(alarmId);
        if (alarm == null || alarm.UserId != userId)
        {
            return NotFound(new { error = "Alarm not found" });
        }

        // Get active cycle
        var activeCycle = await cycleRepository.GetActiveCycleByAlarmIdAsync(alarmId);
        if (activeCycle == null)
        {
            // No active cycle, create a new one
            await cycleService.CreateCycleAsync(alarm);
            logger.LogInformation("User {UserId} pinged alarm {AlarmId}, new cycle created", userId, alarmId);
            return Ok(new { message = "Ping received, new cycle started" });
        }

        // Check if alarm point has passed
        if (DateTimeOffset.UtcNow > activeCycle.AlarmPointTime)
        {
            logger.LogWarning("User {UserId} pinged alarm {AlarmId} after alarm point", userId, alarmId);
            return Ok(new { message = "Ping received after alarm point, alarm may have already triggered" });
        }

        // Create and save ping record
        var ping = new Ping
        {
            Id = Guid.NewGuid(),
            AlarmId = alarmId,
            AlarmCycleId = activeCycle.Id,
            ReceivedAt = DateTimeOffset.UtcNow
        };
        
        await pingRepository.AddAsync(ping);

        // Complete current cycle
        await cycleService.CompleteCycleAsync(activeCycle.Id, ping.Id);

        // Create new cycle if not AllowPingNoTimerReset
        if (!alarm.AllowPingNoTimerReset)
        {
            await cycleService.CreateCycleAsync(alarm, ping);
        }

        logger.LogInformation("User {UserId} successfully pinged alarm {AlarmId}", userId, alarmId);

        return Ok(new
        {
            message = "Ping successful",
            cycleCompleted = true,
            newCycleCreated = !alarm.AllowPingNoTimerReset
        });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
