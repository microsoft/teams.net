using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Extensions.Logging;

public class TeamsLogger : ILogger, IDisposable
{
    public Common.Logging.ILogger Logger => _logger;

    protected Common.Logging.ILogger _logger;

    public TeamsLogger(Common.Logging.ILogger logger)
    {
        _logger = logger;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return default;
    }

    public bool IsEnabled(LogLevel level)
    {
        return _logger.IsEnabled(level.ToTeams());
    }

    public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logger.Log(level.ToTeams(), formatter(state, exception));
    }

    public void Dispose()
    {
        // do nothing
    }

    public ILogger Create(string name)
    {
        return new TeamsLogger(_logger.Create(name));
    }

    public ILogger Child(string name)
    {
        return new TeamsLogger(_logger.Child(name));
    }

    public ILogger Peer(string name)
    {
        return new TeamsLogger(_logger.Peer(name));
    }
}