// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Apps.Extensions;

public class TeamsService : IHostedLifecycleService
{
    protected App _app;
    protected ILogger<App> _logger;

    public TeamsService(App app, ILogger<App> logger)
    {
        _app = app;
        _logger = logger;
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

    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        await _app.Start(cancellationToken);
        _logger.LogDebug("Started");
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