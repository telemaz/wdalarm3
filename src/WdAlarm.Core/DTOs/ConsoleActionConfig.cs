namespace WdAlarm.Core.DTOs;

public class ConsoleActionConfig
{
    public string Payload { get; set; } = string.Empty;
    public string Prefix { get; set; } = "ALARM";
    public bool IncludeTimestamp { get; set; } = true;
    public bool UseErrorStream { get; set; } = false;
}