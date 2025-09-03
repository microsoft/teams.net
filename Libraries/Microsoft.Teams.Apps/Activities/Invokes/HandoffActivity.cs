// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class HandoffAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.Handoff, typeof(HandoffActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<HandoffActivity>();
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnHandoff(this App app, Func<IContext<HandoffActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Handoff]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<HandoffActivity>());
                return null;
            },
            Selector = activity => activity is HandoffActivity
        });

        return app;
    }

    public static App OnHandoff(this App app, Func<IContext<HandoffActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Handoff]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = context => handler(context.ToActivityType<HandoffActivity>()),
            Selector = activity => activity is HandoffActivity
        });

        return app;
    }
}