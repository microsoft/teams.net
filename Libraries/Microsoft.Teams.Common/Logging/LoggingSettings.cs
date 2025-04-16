namespace Microsoft.Teams.Common.Logging;

public class LoggingSettings
{
    public string Enable { get; set; } = "*";
    public LogLevel Level { get; set; } = LogLevel.Info;
}