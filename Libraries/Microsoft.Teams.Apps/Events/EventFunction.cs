// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Events;

/// <summary>
/// a function for emitting events
/// </summary>
/// <param name="plugin">the plugin</param>
/// <param name="@type">the event type</param>
/// <param name="payload">the event payload</param>
public delegate Task<object?> EventFunction(
    IPlugin plugin,
    EventType @type,
    Event? payload = null,
    CancellationToken cancellationToken = default
);