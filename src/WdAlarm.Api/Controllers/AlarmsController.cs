using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WdAlarm.Api.Models.Requests;
using WdAlarm.Api.Models.Responses;
using WdAlarm.Core.Entities;
using WdAlarm.Core.Interfaces;

namespace WdAlarm.Api.Controllers;

/// <summary>
/// Controller for alarm management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlarmsController : ControllerBase
{
    private readonly IAlarmRepository alarmRepository;
    private readonly IAlarmCycleRepository cycleRepository;
    private readonly IAlarmCycleService cycleService;
    private readonly ILogger<AlarmsController> logger;

    public AlarmsController(
        IAlarmRepository alarmRepository,
        IAlarmCycleRepository cycleRepository,
        IAlarmCycleService cycleService,
        ILogger<AlarmsController> logger)
    {
        this.alarmRepository = alarmRepository;
        this.cycleRepository = cycleRepository;
        this.cycleService = cycleService;
        this.logger = logger;
    }

    /// <summary>
    /// Get all alarms for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AlarmResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AlarmResponse>>> GetAlarms()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var alarms = await alarmRepository.GetAllActiveAsync();
        var userAlarms = alarms.Where(a => a.UserId == userId).ToList();

        var response = new List<AlarmResponse>();
        foreach (var alarm in userAlarms)
        {
            var activeCycle = await cycleRepository.GetActiveCycleByAlarmIdAsync(alarm.Id);
            response.Add(MapToResponse(alarm, activeCycle));
        }

        return Ok(response);
    }

    /// <summary>
    /// Get a specific alarm by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AlarmResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlarmResponse>> GetAlarm(Guid id)
    {
        var userId = GetCurrentUserId();
        var alarm = await alarmRepository.GetByIdAsync(id);

        if (alarm == null || alarm.UserId != userId)
        {
            return NotFound();
        }

        var activeCycle = await cycleRepository.GetActiveCycleByAlarmIdAsync(alarm.Id);
        return Ok(MapToResponse(alarm, activeCycle));
    }

    /// <summary>
    /// Create a new alarm
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AlarmResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AlarmResponse>> CreateAlarm([FromBody] CreateAlarmRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var alarm = new Alarm
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            DelayType = request.DelayType,
            TimeoutDuration = request.TimeoutDuration,
            CronExpression = request.CronExpression,
            AllowPingNoTimerReset = request.AllowPingNoTimerReset,
            VerificationMethod = request.VerificationMethod,
            VerificationConfig = request.VerificationConfig,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await alarmRepository.AddAsync(alarm);

        // Create initial cycle
        var cycle = await cycleService.CreateCycleAsync(alarm);

        logger.LogInformation("User {UserId} created alarm {AlarmId}", userId, alarm.Id);

        return CreatedAtAction(nameof(GetAlarm), new { id = alarm.Id }, MapToResponse(alarm, cycle));
    }

    /// <summary>
    /// Update an existing alarm
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(AlarmResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlarmResponse>> UpdateAlarm(Guid id, [FromBody] CreateAlarmRequest request)
    {
        var userId = GetCurrentUserId();
        var alarm = await alarmRepository.GetByIdAsync(id);

        if (alarm == null || alarm.UserId != userId)
        {
            return NotFound();
        }

        alarm.Name = request.Name;
        alarm.Description = request.Description;
        alarm.DelayType = request.DelayType;
        alarm.TimeoutDuration = request.TimeoutDuration;
        alarm.CronExpression = request.CronExpression;
        alarm.AllowPingNoTimerReset = request.AllowPingNoTimerReset;
        alarm.VerificationMethod = request.VerificationMethod;
        alarm.VerificationConfig = request.VerificationConfig;
        alarm.UpdatedAt = DateTimeOffset.UtcNow;

        await alarmRepository.UpdateAsync(alarm);

        // Cancel existing cycle and create new one
        var activeCycle = await cycleRepository.GetActiveCycleByAlarmIdAsync(alarm.Id);
        if (activeCycle != null)
        {
            await cycleService.CancelCycleAsync(activeCycle.Id, "Alarm configuration changed");
        }

        var newCycle = await cycleService.CreateCycleAsync(alarm);

        logger.LogInformation("User {UserId} updated alarm {AlarmId}", userId, alarm.Id);

        return Ok(MapToResponse(alarm, newCycle));
    }

    /// <summary>
    /// Delete an alarm
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAlarm(Guid id)
    {
        var userId = GetCurrentUserId();
        var alarm = await alarmRepository.GetByIdAsync(id);

        if (alarm == null || alarm.UserId != userId)
        {
            return NotFound();
        }

        // Cancel active cycle
        var activeCycle = await cycleRepository.GetActiveCycleByAlarmIdAsync(alarm.Id);
        if (activeCycle != null)
        {
            await cycleService.CancelCycleAsync(activeCycle.Id, "Alarm deleted");
        }

        await alarmRepository.DeleteAsync(id);

        logger.LogInformation("User {UserId} deleted alarm {AlarmId}", userId, alarm.Id);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private static AlarmResponse MapToResponse(Alarm alarm, AlarmCycle? activeCycle)
    {
        return new AlarmResponse
        {
            Id = alarm.Id,
            Name = alarm.Name,
            Description = alarm.Description,
            DelayType = alarm.DelayType,
            TimeoutDuration = alarm.TimeoutDuration,
            CronExpression = alarm.CronExpression,
            AllowPingNoTimerReset = alarm.AllowPingNoTimerReset,
            VerificationMethod = alarm.VerificationMethod,
            IsActive = alarm.IsActive,
            CreatedAt = alarm.CreatedAt,
            UpdatedAt = alarm.UpdatedAt,
            HasActiveCycle = activeCycle != null,
            NextAlarmPoint = activeCycle?.AlarmPointTime
        };
    }
}
