using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Events;

/// <summary>
/// a function for emitting events
/// </summary>
/// <param name="plugin">the plugin</param>
/// <param name="name">the event name</param>
/// <param name="payload">the event payload</param>
public delegate Task<object?> EventFunction(
    IPlugin plugin,
    string name,
    Event? payload = null,
    CancellationToken cancellationToken = default
);