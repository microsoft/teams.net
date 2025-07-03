// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Events;

public static partial class AppEventExtensions
{
    public static App OnStart(this App app, Action<IPlugin, Event> handler)
    {
        return app.OnEvent(EventType.Start, handler);
    }

    public static App OnStart(this App app, Func<IPlugin, Event, CancellationToken, Task> handler)
    {
        return app.OnEvent(EventType.Start, handler);
    }
}