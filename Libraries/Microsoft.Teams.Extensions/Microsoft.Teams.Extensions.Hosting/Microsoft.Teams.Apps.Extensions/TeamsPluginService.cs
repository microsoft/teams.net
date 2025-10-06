// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Extensions;

public class TeamsPluginService<TPlugin> : IHostedLifecycleService where TPlugin : IPlugin
{
    protected App _app;
    protected TPlugin _plugin;
    protected ILogger<TPlugin> _logger;

    public TeamsPluginService(App app, TPlugin plugin, ILogger<TPlugin> logger)
    {
        _app = app;
        _plugin = plugin;
        _logger = logger;
        _app.AddPlugin(plugin);
    }

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Start");
        return Task.CompletedTask;
    }

    public Task StartedAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Started");
        return Task.CompletedTask;
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Stopping");
        var src = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        src.Cancel();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Stop");
        return Task.CompletedTask;
    }

    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Stopped");
        return Task.CompletedTask;
    }
}