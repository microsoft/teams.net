// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Represents a live Socket Mode transport connection.
/// </summary>
internal interface ISocketConnection : IAsyncDisposable
{
    /// <summary>
    /// Gets the remaining lifetime of the connection token, when available.
    /// </summary>
    TimeSpan? TokenLifetime { get; }

    /// <summary>
    /// Starts the connection.
    /// </summary>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the connection.
    /// </summary>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Creates Socket Mode transport connections.
/// </summary>
internal interface ISocketConnectionFactory
{
    /// <summary>
    /// Creates a connection for the specified negotiate endpoint and handlers.
    /// </summary>
    /// <param name="negotiateUri">The endpoint used to negotiate the connection.</param>
    /// <param name="handlers">Callbacks for frames and connection closure.</param>
    /// <returns>A new Socket Mode connection.</returns>
    ISocketConnection Create(
        Uri negotiateUri,
        SocketConnectionHandlers handlers);
}

/// <summary>
/// Defines callbacks used by a Socket Mode connection.
/// </summary>
internal sealed class SocketConnectionHandlers
{
    /// <summary>
    /// Initializes the connection callbacks.
    /// </summary>
    /// <param name="onActivity">Handles an incoming activity envelope.</param>
    /// <param name="onReady">Handles the ready frame.</param>
    /// <param name="onClosed">Handles connection closure.</param>
    internal SocketConnectionHandlers(
        Func<SocketActivityEnvelope, Task<SocketReplyFrame?>> onActivity,
        Action<SocketReadyFrame> onReady,
        Action<Exception?> onClosed)
    {
        OnActivity = onActivity ?? throw new ArgumentNullException(nameof(onActivity));
        OnReady = onReady ?? throw new ArgumentNullException(nameof(onReady));
        OnClosed = onClosed ?? throw new ArgumentNullException(nameof(onClosed));
    }

    /// <summary>
    /// Gets the incoming activity callback.
    /// </summary>
    internal Func<SocketActivityEnvelope, Task<SocketReplyFrame?>> OnActivity { get; }

    /// <summary>
    /// Gets the ready-frame callback.
    /// </summary>
    internal Action<SocketReadyFrame> OnReady { get; }

    /// <summary>
    /// Gets the connection-closed callback.
    /// </summary>
    internal Action<Exception?> OnClosed { get; }
}
