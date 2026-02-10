namespace WdAlarm.Core.DTOs;

public class RestApiActionConfig
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "POST";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string> QueryParameters { get; set; } = new();
}