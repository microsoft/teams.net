// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.WebSockets;

namespace Microsoft.Teams.Bot.DevTools.Extensions;

/// <summary>
/// Extension methods for WebSocket.
/// </summary>
public static class WebSocketExtensions
{
    /// <summary>
    /// Returns true if the WebSocket can be closed (not already closed or aborted).
    /// </summary>
    public static bool IsCloseable(this WebSocket socket)
    {
        ArgumentNullException.ThrowIfNull(socket);

        return socket.State != WebSocketState.Closed
            && socket.State != WebSocketState.Aborted;
    }
}
