// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class TypingAttribute() : ActivityAttribute(ActivityType.Typing, typeof(TypingActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<TypingActivity>();
}

public static partial class AppActivityExtensions
{
    public static App OnTyping(this App app, Func<IContext<TypingActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.Typing,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<TypingActivity>());
                return null;
            },
            Selector = activity => activity is TypingActivity
        });

        return app;
    }
}