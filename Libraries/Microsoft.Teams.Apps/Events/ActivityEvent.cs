// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Events;

public class ActivityEvent : Event
{
    public required IToken Token { get; set; }
    public required IActivity Activity { get; set; }
    public IDictionary<string, object>? ContextExtra { get; set; }
}

public static partial class AppEventExtensions
{
    public static App OnActivity(this App app, Action<ISenderPlugin, ActivityEvent> handler)
    {
        return app.OnEvent(EventType.Activity, (plugin, @event) => handler((ISenderPlugin)plugin, (ActivityEvent)@event));
    }

    public static App OnActivity(this App app, Func<ISenderPlugin, ActivityEvent, CancellationToken, Task> handler)
    {
        return app.OnEvent(EventType.Activity, (plugin, @event, token) => handler((ISenderPlugin)plugin, (ActivityEvent)@event, token));
    }
}