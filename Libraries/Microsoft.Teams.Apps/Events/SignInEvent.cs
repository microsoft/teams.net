// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Events;

public class SignInEvent : Event
{
    public required Microsoft.Teams.Api.Token.Response Token { get; set; }

    public required IContext<SignInActivity> Context { get; set; }
}

public static partial class AppEventExtensions
{
    public static App OnSignIn(this App app, Action<ISenderPlugin, SignInEvent> handler)
    {
        return app.OnEvent(EventType.SignIn, (plugin, @event) => handler((ISenderPlugin)plugin, (SignInEvent)@event));
    }

    public static App OnSignIn(this App app, Func<ISenderPlugin, SignInEvent, CancellationToken, Task> handler)
    {
        return app.OnEvent(EventType.SignIn, (plugin, @event, token) => handler((ISenderPlugin)plugin, (SignInEvent)@event, token));
    }
}