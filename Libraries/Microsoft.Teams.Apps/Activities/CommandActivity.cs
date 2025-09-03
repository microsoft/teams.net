// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class CommandAttribute() : ActivityAttribute(ActivityType.Command, type: typeof(CommandActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<CommandActivity>();
}

public static partial class AppActivityExtensions
{
    public static App OnCommand(this App app, Func<IContext<CommandActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.Command,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<CommandActivity>());
                return null;
            },
            Selector = activity => activity is CommandActivity
        });

        return app;
    }
}