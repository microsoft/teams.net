using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Apps.Extensions;

public class TeamsService : IHostedLifecycleService
{
    protected IApp _app;
    protected ILogger<TeamsService> _logger;

    public TeamsService(IApp app, ILogger<TeamsService> logger)
    {
        _app = app;
        _logger = logger;
    }

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => _logger.LogDebug("Starting"));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => _logger.LogDebug("Start"));
    }

    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        await _app.Start();
        _logger.LogDebug("Started");
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => _logger.LogDebug("Stopping"));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => _logger.LogDebug("Stop"));
    }

    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => _logger.LogDebug("Stopped"));
    }
}