// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.WebSockets;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;

public static class WebSocketExtensions
{
    public static bool IsCloseable(this WebSocket socket)
    {
        return (
            socket.State != WebSocketState.Closed &&
            socket.State != WebSocketState.Aborted
        );
    }
}