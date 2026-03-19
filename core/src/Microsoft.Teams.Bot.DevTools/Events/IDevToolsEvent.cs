// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Bot.DevTools.Events;

/// <summary>
/// Base interface for all DevTools events sent to WebSocket clients.
/// </summary>
public interface IDevToolsEvent
{
    /// <summary>
    /// Unique identifier for this event.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Event type discriminator (e.g. "activity.received", "metadata").
    /// </summary>
    string Type { get; }

    /// <summary>
    /// Event payload.
    /// </summary>
    object? Body { get; }

    /// <summary>
    /// When this event was created.
    /// </summary>
    DateTime SentAt { get; }
}
