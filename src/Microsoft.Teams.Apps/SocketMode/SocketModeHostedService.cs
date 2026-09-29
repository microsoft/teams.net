// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Runs the Socket Mode transport for the lifetime of the host.
/// </summary>
/// <remarks>
/// Startup waits until every geo is ready, so the host does not report started while the bot cannot receive
/// activities, and a startup failure surfaces from <c>host.Run()</c> instead of being swallowed in the background.
/// </remarks>
/// <param name="services">The host's service provider, used to create the transport and detect a web server.</param>
internal sealed class SocketModeHostedService(IServiceProvider services) : IHostedService
{
    private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));
    private SocketModeTransport? _transport;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Socket Mode replaces inbound HTTP rather than running beside it, so a host that would also start a web
        // server (for example a WebApplication) is rejected before any socket opens.
        if (_services.GetService<IServiceProviderIsService>()?.IsService(typeof(IServer)) == true)
        {
            throw new InvalidOperationException(TeamsBotApplicationHostingExtensions.SocketWithWebServerMessage);
        }

        // Created here rather than injected, so invalid options fail when the host starts, as in the other SDKs.
        _transport = _services.GetRequiredService<SocketModeTransport>();
        return _transport.StartAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
        => _transport is null ? Task.CompletedTask : _transport.StopAsync().WaitAsync(cancellationToken);
}
