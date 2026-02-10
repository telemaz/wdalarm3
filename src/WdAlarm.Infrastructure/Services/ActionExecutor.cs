using System.Text.Json;
using Microsoft.Extensions.Logging;
using WdAlarm.Core.DTOs;
using WdAlarm.Core.Entities;
using WdAlarm.Core.Enums;
using WdAlarm.Core.Interfaces;

namespace WdAlarm.Infrastructure.Services;

public class ActionExecutor : IActionExecutor
{
    private readonly ILogger<ActionExecutor> _logger;

    public ActionExecutor(ILogger<ActionExecutor> logger)
    {
        _logger = logger;
    }

    public async Task<ActionExecutionResult> ExecuteAsync(ActionExecution actionExecution, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Executing action {ActionId} of type {ActionType} scheduled at {ScheduledTime}",
                actionExecution.Id,
                actionExecution.ActionType,
                actionExecution.ScheduledTime);

            var result = actionExecution.ActionType switch
            {
                ActionType.Email => await ExecuteEmailActionAsync(actionExecution, cancellationToken),
                ActionType.SMS => await ExecuteSmsActionAsync(actionExecution, cancellationToken),
                ActionType.RestApi => await ExecuteRestApiActionAsync(actionExecution, cancellationToken),
                ActionType.Console => await ExecuteConsoleActionAsync(actionExecution, cancellationToken),
                _ => new ActionExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Unsupported action type: {actionExecution.ActionType}",
                    IsRetriable = false
                }
            };

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Action {ActionId} executed successfully",
                    actionExecution.Id);
            }
            else
            {
                _logger.LogWarning(
                    "Action {ActionId} execution failed: {Error}",
                    actionExecution.Id,
                    result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception executing action {ActionId}", actionExecution.Id);
            return new ActionExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                IsRetriable = true
            };
        }
    }

    private async Task<ActionExecutionResult> ExecuteEmailActionAsync(ActionExecution actionExecution, CancellationToken cancellationToken)
    {
        try
        {
            var actionConfigJson = actionExecution.AlarmDelayAction?.ActionConfig 
                                ?? actionExecution.AlarmPingAction?.ActionConfig;
            
            if (string.IsNullOrEmpty(actionConfigJson))
            {
                return new ActionExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Action configuration not found",
                    IsRetriable = false
                };
            }

            var emailConfig = JsonSerializer.Deserialize<EmailActionConfig>(actionConfigJson);
            if (emailConfig == null)
            {
                return new ActionExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid email action configuration",
                    IsRetriable = false
                };
            }

            _logger.LogInformation(
                "EMAIL ACTION - To: {To}, Subject: {Subject}, SMTP: {SmtpHost}:{SmtpPort}",
                emailConfig.To,
                emailConfig.Subject,
                emailConfig.SmtpHost,
                emailConfig.SmtpPort);

            // TODO: Implement actual email sending using MailKit
            await Task.Delay(10, cancellationToken);

            var responseData = JsonSerializer.Serialize(new
            {
                SentTo = emailConfig.To,
                Subject = emailConfig.Subject,
                SentAt = DateTimeOffset.UtcNow
            });

            return new ActionExecutionResult
            {
                IsSuccess = true,
                IsRetriable = false,
                ResponseData = responseData
            };
        }
        catch (Exception ex)
        {
            return new ActionExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = $"Email action failed: {ex.Message}",
                IsRetriable = true
            };
        }
    }

    private async Task<ActionExecutionResult> ExecuteSmsActionAsync(ActionExecution actionExecution, CancellationToken cancellationToken)
    {
        try
        {
            var actionConfigJson = actionExecution.AlarmDelayAction?.ActionConfig 
                                ?? actionExecution.AlarmPingAction?.ActionConfig;
            
            if (string.IsNullOrEmpty(actionConfigJson))
            {
                return new ActionExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Action configuration not found",
                    IsRetriable = false
                };
            }

            var smsConfig = JsonSerializer.Deserialize<SmsActionConfig>(actionConfigJson);
            if (smsConfig == null)
            {
                return new ActionExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid SMS action configuration",
                    IsRetriable = false
                };
            }

            _logger.LogInformation(
                "SMS ACTION - Phone: {PhoneNumber}, Provider: {Provider}, Message: {Message}",
                smsConfig.PhoneNumber,
                smsConfig.Provider,
                smsConfig.Message);

            // TODO: Implement actual SMS sending based on provider
            await Task.Delay(10, cancellationToken);

            var responseData = JsonSerializer.Serialize(new
            {
                SentTo = smsConfig.PhoneNumber,
                Provider = smsConfig.Provider,
                SentAt = DateTimeOffset.UtcNow
            });

            return new ActionExecutionResult
            {
                IsSuccess = true,
                IsRetriable = false,
                ResponseData = responseData
            };
        }
        catch (Exception ex)
        {
            return new ActionExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = $"SMS action failed: {ex.Message}",
                IsRetriable = true
            };
        }
    }

    private async Task<ActionExecutionResult> ExecuteRestApiActionAsync(ActionExecution actionExecution, CancellationToken cancellationToken)
    {
        try
        {
            var actionConfigJson = actionExecution.AlarmDelayAction?.ActionConfig 
                                ?? actionExecution.AlarmPingAction?.ActionConfig;
            
            if (string.IsNullOrEmpty(actionConfigJson))
            {
                return new ActionExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Action configuration not found",
                    IsRetriable = false
                };
            }

            var restConfig = JsonSerializer.Deserialize<RestApiActionConfig>(actionConfigJson);
            if (restConfig == null)
            {
                return new ActionExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid REST API action configuration",
                    IsRetriable = false
                };
            }

            _logger.LogInformation(
                "REST API ACTION - {Method} {Url}, Headers: {HeadersCount}, QueryParams: {QueryParamsCount}",
                restConfig.Method,
                restConfig.Url,
                restConfig.Headers.Count,
                restConfig.QueryParameters.Count);

            // TODO: Implement actual HTTP call using HttpClient
            await Task.Delay(10, cancellationToken);

            var responseData = JsonSerializer.Serialize(new
            {
                Url = restConfig.Url,
                Method = restConfig.Method,
                ExecutedAt = DateTimeOffset.UtcNow,
                MockResponse = "Action executed (mock implementation)"
            });

            return new ActionExecutionResult
            {
                IsSuccess = true,
                IsRetriable = false,
                ResponseData = responseData
            };
        }
        catch (Exception ex)
        {
            return new ActionExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = $"REST API action failed: {ex.Message}",
                IsRetriable = true
            };
        }
    }

    private async Task<ActionExecutionResult> ExecuteConsoleActionAsync(ActionExecution actionExecution, CancellationToken cancellationToken)
    {
        try
        {
            var actionConfigJson = actionExecution.AlarmDelayAction?.ActionConfig 
                                ?? actionExecution.AlarmPingAction?.ActionConfig;
            
            if (string.IsNullOrEmpty(actionConfigJson))
            {
                return new ActionExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Action configuration not found",
                    IsRetriable = false
                };
            }

            var consoleConfig = JsonSerializer.Deserialize<ConsoleActionConfig>(actionConfigJson);
            if (consoleConfig == null)
            {
                return new ActionExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid console action configuration",
                    IsRetriable = false
                };
            }

            // Build the console output
            var output = new System.Text.StringBuilder();
            
            if (consoleConfig.IncludeTimestamp)
            {
                output.Append($"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] ");
            }
            
            output.Append($"[{consoleConfig.Prefix}] ");
            output.Append(consoleConfig.Payload);

            var outputString = output.ToString();

            // Write to console (standard output or error stream)
            if (consoleConfig.UseErrorStream)
            {
                Console.Error.WriteLine(outputString);
            }
            else
            {
                Console.WriteLine(outputString);
            }

            _logger.LogInformation(
                "CONSOLE ACTION - Prefix: {Prefix}, UseErrorStream: {UseErrorStream}, IncludeTimestamp: {IncludeTimestamp}",
                consoleConfig.Prefix,
                consoleConfig.UseErrorStream,
                consoleConfig.IncludeTimestamp);

            await Task.Delay(10, cancellationToken);

            var responseData = JsonSerializer.Serialize(new
            {
                Output = outputString,
                UseErrorStream = consoleConfig.UseErrorStream,
                ExecutedAt = DateTimeOffset.UtcNow
            });

            return new ActionExecutionResult
            {
                IsSuccess = true,
                IsRetriable = false,
                ResponseData = responseData
            };
        }
        catch (Exception ex)
        {
            return new ActionExecutionResult
            {
                IsSuccess = false,
                ErrorMessage = $"Console action failed: {ex.Message}",
                IsRetriable = true
            };
        }
    }
}