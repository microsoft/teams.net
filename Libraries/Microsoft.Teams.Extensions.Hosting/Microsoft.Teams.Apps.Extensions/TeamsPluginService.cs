using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Extensions;

public class TeamsPluginService<TPlugin> : IHostedLifecycleService where TPlugin : IPlugin
{
    protected TPlugin _plugin;
    protected ILogger<TeamsPluginService<TPlugin>> _logger;

    public TeamsPluginService(TPlugin plugin, ILogger<TeamsPluginService<TPlugin>> logger)
    {
        _plugin = plugin;
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

    public Task StartedAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => _logger.LogDebug("Started"));
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