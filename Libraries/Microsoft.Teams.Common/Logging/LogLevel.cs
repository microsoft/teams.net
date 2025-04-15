using Microsoft.Teams.Common.Text;

namespace Microsoft.Teams.Common.Logging;

public enum LogLevel
{
    Error = 0,
    Warn = 1,
    Info = 2,
    Debug = 3
}

public static class LogLevelExtensions
{
    public static LogLevel? ToLogLevel(this string text)
    {
        return text.ToLower() switch
        {
            "error" => LogLevel.Error,
            "warn" => LogLevel.Warn,
            "info" => LogLevel.Info,
            "debug" => LogLevel.Debug,
            _ => null
        };
    }

    public static ANSI Color(this LogLevel level)
    {
        return level == LogLevel.Error ? ANSI.ForegroundRed
             : level == LogLevel.Warn ? ANSI.ForegroundYellow
             : level == LogLevel.Info ? ANSI.ForegroundCyan
             : ANSI.ForegroundMagenta;
    }

    public static string? ToString(this LogLevel level)
    {
        return level switch
        {
            LogLevel.Error => "error",
            LogLevel.Warn => "warn",
            LogLevel.Info => "info",
            LogLevel.Debug => "debug",
            _ => Enum.GetName(level)
        };
    }
}