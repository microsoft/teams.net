// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Events;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class EventAttribute : ActivityAttribute
{
    public EventAttribute() : base(ActivityType.Event, typeof(EventActivity))
    {

    }

    public EventAttribute(Name name) : base(string.Join("/", [ActivityType.Event, name]), typeof(EventActivity))
    {

    }

    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<EventActivity>();
}

public static partial class AppEventActivityExtensions
{
    public static App OnEvent(this App app, Func<IContext<EventActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.Event,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<EventActivity>());
                return null;
            },
            Selector = activity => activity is EventActivity
        });

        return app;
    }
}