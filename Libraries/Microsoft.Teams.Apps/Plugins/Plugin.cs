// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Events;

namespace Microsoft.Teams.Apps.Plugins;

/// <summary>
/// a component for extending the base
/// `App` functionality
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// emitted when the plugin encounters an error
    /// </summary>
    public event EventFunction Events;

    /// <summary>
    /// lifecycle method called by the `App` once during initialization
    /// </summary>
    public Task OnInit(App app, CancellationToken cancellationToken = default);

    /// <summary>
    /// lifecycle method called by the `App` once during startup
    /// </summary>
    public Task OnStart(App app, CancellationToken cancellationToken = default);

    /// <summary>
    /// called by the `App` when an error occurs
    /// </summary>
    public Task OnError(App app, IPlugin plugin, ErrorEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// called by the `App` when an activity is received
    /// </summary>
    public Task OnActivity(App app, ISenderPlugin sender, ActivityEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// called by the `App` when an activity is sent
    /// </summary>
    public Task OnActivitySent(App app, ISenderPlugin sender, ActivitySentEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// called by the `App` when an activity response is sent
    /// </summary>
    public Task OnActivityResponse(App app, ISenderPlugin sender, ActivityResponseEvent @event, CancellationToken cancellationToken = default);
}