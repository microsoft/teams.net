// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Hosting;

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Runs the Socket Mode transport for the lifetime of the host.
/// </summary>
/// <remarks>
/// Startup waits until every geo is ready, so the host does not report started while the bot cannot receive
/// activities, and a startup failure surfaces from <c>app.Run()</c> instead of being swallowed in the background.
/// </remarks>
/// <param name="transport">The transport to run.</param>
internal sealed class SocketModeHostedService(SocketModeTransport transport) : IHostedService
{
    private readonly SocketModeTransport _transport = transport ?? throw new ArgumentNullException(nameof(transport));

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => _transport.StartAsync(cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => _transport.StopAsync().WaitAsync(cancellationToken);
}
