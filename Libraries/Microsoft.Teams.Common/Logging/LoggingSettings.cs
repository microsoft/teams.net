namespace Microsoft.Teams.Common.Logging;

public class LoggingSettings
{
    public string Enable { get; init; } = "*";
    public LogLevel Level { get; init; } = LogLevel.Info;
}