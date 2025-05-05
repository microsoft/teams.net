using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Extensions.Logging;

[ProviderAlias("Microsoft.Teams")]
public class TeamsLoggerProvider : ILoggerProvider, IDisposable
{
    protected TeamsLogger _logger;

    public TeamsLoggerProvider(Common.Logging.ILogger logger)
    {
        _logger = new TeamsLogger(logger);
    }

    public TeamsLoggerProvider(TeamsLogger logger)
    {
        _logger = logger;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _logger.BeginScope(state);
    }

    public ILogger CreateLogger<T>()
    {
        var name = typeof(T).Name;
        return _logger.Create(name);
    }

    public ILogger CreateLogger(string name)
    {
        return _logger.Create(name);
    }

    public void Dispose()
    {
        _logger.Dispose();
    }
}