namespace WdAlarm.Core.DTOs;

public class ActionExecutionResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsRetriable { get; set; }
    public string? ResponseData { get; set; }
}
