// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Teams.Bot.DevTools.Events;
using Microsoft.Teams.Bot.DevTools.Models;

namespace Microsoft.Teams.Bot.DevTools;

/// <summary>
/// Shared singleton service that holds DevTools state: WebSocket connections, metadata, and emit helpers.
/// </summary>
public class DevToolsService
{
    /// <summary>
    /// Active WebSocket connections to DevTools UI clients.
    /// </summary>
    public WebSocketCollection Sockets { get; } = new();

    /// <summary>
    /// DevTools configuration settings.
    /// </summary>
    public DevToolsSettings Settings { get; }

    /// <summary>
    /// The bot application ID (populated at startup from BotApplicationOptions).
    /// </summary>
    public string? AppId { get; set; }

    /// <summary>
    /// The bot application name.
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// Builds metadata for the current app.
    /// </summary>
    public DevToolsMetaData MetaData
    {
        get
        {
            var meta = new DevToolsMetaData { Id = AppId, Name = AppName };
            foreach (var page in Settings.Pages)
            {
                meta.Pages.Add(page);
            }

            return meta;
        }
    }

    /// <summary>
    /// Creates a new DevToolsService with the given settings.
    /// </summary>
    public DevToolsService(DevToolsSettings settings)
    {
        Settings = settings;
    }

    /// <summary>
    /// Emit a "received" activity event to all connected DevTools clients.
    /// </summary>
    public Task EmitReceived(CoreActivity activity, CancellationToken cancellationToken = default)
        => Sockets.Emit(ActivityEvent.Received(activity), cancellationToken);

    /// <summary>
    /// Emit a "sent" activity event to all connected DevTools clients.
    /// </summary>
    public Task EmitSent(CoreActivity activity, CancellationToken cancellationToken = default)
        => Sockets.Emit(ActivityEvent.Sent(activity), cancellationToken);

    /// <summary>
    /// Emit an "error" activity event to all connected DevTools clients.
    /// </summary>
    public Task EmitError(CoreActivity activity, object error, CancellationToken cancellationToken = default)
        => Sockets.Emit(ActivityEvent.Err(activity, error), cancellationToken);
}
