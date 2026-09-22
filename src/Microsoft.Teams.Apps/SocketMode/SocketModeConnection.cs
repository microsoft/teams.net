// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.SocketMode;

internal interface ISocketConnection : IAsyncDisposable
{
    TimeSpan? TokenLifetime { get; }

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken = default);
}

internal interface ISocketConnectionFactory
{
    ISocketConnection Create(
        Uri negotiateUri,
        SocketConnectionHandlers handlers);
}

internal sealed class SocketConnectionHandlers
{
    internal SocketConnectionHandlers(
        Func<SocketActivityEnvelope, Task<SocketReplyFrame?>> onActivity,
        Action<SocketReadyFrame> onReady,
        Action<Exception?> onClosed)
    {
        OnActivity = onActivity ?? throw new ArgumentNullException(nameof(onActivity));
        OnReady = onReady ?? throw new ArgumentNullException(nameof(onReady));
        OnClosed = onClosed ?? throw new ArgumentNullException(nameof(onClosed));
    }

    internal Func<SocketActivityEnvelope, Task<SocketReplyFrame?>> OnActivity { get; }

    internal Action<SocketReadyFrame> OnReady { get; }

    internal Action<Exception?> OnClosed { get; }
}
