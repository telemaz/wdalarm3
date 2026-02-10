using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using WdAlarm.Core.DTOs;
using WdAlarm.Core.Entities;
using WdAlarm.Core.Enums;
using WdAlarm.Core.Interfaces;
using WdAlarm.Infrastructure.Services;

namespace WdAlarm.UnitTests.Services;

public class ActionExecutorTests
{
    private readonly Mock<ILogger<ActionExecutor>> _mockLogger;
    private readonly IActionExecutor _actionExecutor;

    public ActionExecutorTests()
    {
        _mockLogger = new Mock<ILogger<ActionExecutor>>();
        _actionExecutor = new ActionExecutor(_mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmailAction_ShouldLogExecutionDetails()
    {
        // Arrange
        var actionExecution = new ActionExecution
        {
            Id = Guid.NewGuid(),
            ActionType = ActionType.Email,
            ScheduledTime = DateTimeOffset.UtcNow,
            AlarmDelayAction = new AlarmDelayAction
            {
                ActionConfig = JsonSerializer.Serialize(new EmailActionConfig
                {
                    To = "test@example.com",
                    Subject = "Test Alarm",
                    SmtpHost = "smtp.example.com",
                    SmtpPort = 587,
                    Body = "Test email body"
                })
            }
        };

        // Act
        var result = await _actionExecutor.ExecuteAsync(actionExecution);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsRetriable);
        Assert.NotNull(result.ResponseData);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("EMAIL ACTION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithSmsAction_ShouldLogExecutionDetails()
    {
        // Arrange
        var actionExecution = new ActionExecution
        {
            Id = Guid.NewGuid(),
            ActionType = ActionType.SMS,
            ScheduledTime = DateTimeOffset.UtcNow,
            AlarmPingAction = new AlarmPingAction
            {
                ActionConfig = JsonSerializer.Serialize(new SmsActionConfig
                {
                    PhoneNumber = "+1234567890",
                    Provider = "TestProvider",
                    Message = "Test SMS message"
                })
            }
        };

        // Act
        var result = await _actionExecutor.ExecuteAsync(actionExecution);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsRetriable);
        Assert.NotNull(result.ResponseData);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("SMS ACTION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithRestApiAction_ShouldLogExecutionDetails()
    {
        // Arrange
        var actionExecution = new ActionExecution
        {
            Id = Guid.NewGuid(),
            ActionType = ActionType.RestApi,
            ScheduledTime = DateTimeOffset.UtcNow,
            AlarmDelayAction = new AlarmDelayAction
            {
                ActionConfig = JsonSerializer.Serialize(new RestApiActionConfig
                {
                    Url = "https://api.example.com/webhook",
                    Method = "POST",
                    Body = "{\"message\": \"Test\"}",
                    Headers = new Dictionary<string, string>
                    {
                        ["Authorization"] = "Bearer token"
                    }
                })
            }
        };

        // Act
        var result = await _actionExecutor.ExecuteAsync(actionExecution);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsRetriable);
        Assert.NotNull(result.ResponseData);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("REST API ACTION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnsupportedActionType_ShouldReturnFailure()
    {
        // Arrange
        var actionExecution = new ActionExecution
        {
            Id = Guid.NewGuid(),
            ActionType = (ActionType)999, // Invalid action type
            ScheduledTime = DateTimeOffset.UtcNow
        };

        // Act
        var result = await _actionExecutor.ExecuteAsync(actionExecution);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(result.IsRetriable);
        Assert.Contains("Unsupported action type", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithConsoleAction_ShouldLogToConsole()
    {
        // Arrange
        var actionExecution = new ActionExecution
        {
            Id = Guid.NewGuid(),
            ActionType = ActionType.Console,
            ScheduledTime = DateTimeOffset.UtcNow,
            AlarmDelayAction = new AlarmDelayAction
            {
                ActionConfig = JsonSerializer.Serialize(new ConsoleActionConfig
                {
                    Payload = "Test alarm payload",
                    Prefix = "TEST_ALARM",
                    IncludeTimestamp = true,
                    UseErrorStream = false
                })
            }
        };

        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            var result = await _actionExecutor.ExecuteAsync(actionExecution);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.IsRetriable);
            Assert.NotNull(result.ResponseData);

            var consoleOutput = stringWriter.ToString();
            Assert.Contains("[TEST_ALARM]", consoleOutput);
            Assert.Contains("Test alarm payload", consoleOutput);
            Assert.Contains(DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), consoleOutput);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("CONSOLE ACTION")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            Console.SetOut(originalOut);
            stringWriter.Dispose();
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithConsoleActionToErrorStream_ShouldLogToErrorStream()
    {
        // Arrange
        var actionExecution = new ActionExecution
        {
            Id = Guid.NewGuid(),
            ActionType = ActionType.Console,
            ScheduledTime = DateTimeOffset.UtcNow,
            AlarmPingAction = new AlarmPingAction
            {
                ActionConfig = JsonSerializer.Serialize(new ConsoleActionConfig
                {
                    Payload = "Error payload",
                    Prefix = "ERROR",
                    IncludeTimestamp = false,
                    UseErrorStream = true
                })
            }
        };

        // Capture console error output
        var originalError = Console.Error;
        var stringWriter = new StringWriter();
        Console.SetError(stringWriter);

        try
        {
            // Act
            var result = await _actionExecutor.ExecuteAsync(actionExecution);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.IsRetriable);

            var errorOutput = stringWriter.ToString();
            Assert.Contains("[ERROR]", errorOutput);
            Assert.Contains("Error payload", errorOutput);
        }
        finally
        {
            Console.SetError(originalError);
            stringWriter.Dispose();
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingConfiguration_ShouldReturnFailure()
    {
        // Arrange
        var actionExecution = new ActionExecution
        {
            Id = Guid.NewGuid(),
            ActionType = ActionType.Email,
            ScheduledTime = DateTimeOffset.UtcNow,
            AlarmDelayAction = new AlarmDelayAction
            {
                ActionConfig = "" // Empty configuration
            }
        };

        // Act
        var result = await _actionExecutor.ExecuteAsync(actionExecution);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(result.IsRetriable);
        Assert.Contains("Action configuration not found", result.ErrorMessage);
    }
}