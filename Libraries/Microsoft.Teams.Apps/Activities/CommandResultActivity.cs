// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class CommandResultAttribute() : ActivityAttribute(ActivityType.CommandResult, typeof(CommandResultActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<CommandResultActivity>();
}

public static partial class AppActivityExtensions
{
    public static App OnCommandResult(this App app, Func<IContext<CommandResultActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.CommandResult,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<CommandResultActivity>());
                return null;
            },
            Selector = activity => activity is CommandResultActivity
        });

        return app;
    }

    public static App OnCommandResult(this App app, Func<IContext<CommandResultActivity>, CancellationToken, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.CommandResult,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<CommandResultActivity>(), context.CancellationToken);
                return null;
            },
            Selector = activity => activity is CommandResultActivity
        });

        return app;
    }
}