// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;

/// <summary>
/// Logs a startup warning that the bot will accept unauthenticated requests.
/// Registered by AddTeamsTokenAuthentication only when Teams:ClientId is not
/// configured, so its presence in the service collection is itself the signal.
/// </summary>
internal sealed class AnonymousModeWarningHostedService : IHostedService
{
    private readonly ILogger<AnonymousModeWarningHostedService> _logger;

    public AnonymousModeWarningHostedService(ILogger<AnonymousModeWarningHostedService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "No Teams:ClientId configured. Bot will accept unauthenticated requests on /api/messages.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
