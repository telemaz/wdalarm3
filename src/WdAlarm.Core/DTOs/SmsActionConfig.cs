namespace WdAlarm.Core.DTOs;

public class SmsActionConfig
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public Dictionary<string, string> ProviderConfig { get; set; } = new();
}