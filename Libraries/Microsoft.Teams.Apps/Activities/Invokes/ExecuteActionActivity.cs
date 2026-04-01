// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class ExecuteActionAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.ExecuteAction, typeof(ExecuteActionActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<ExecuteActionActivity>();
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnExecuteAction(this App app, Func<IContext<ExecuteActionActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.ExecuteAction]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<ExecuteActionActivity>());
                return null;
            },
            Selector = activity => activity is ExecuteActionActivity
        });

        return app;
    }

    public static App OnExecuteAction(this App app, Func<IContext<ExecuteActionActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.ExecuteAction]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = context => handler(context.ToActivityType<ExecuteActionActivity>()),
            Selector = activity => activity is ExecuteActionActivity
        });

        return app;
    }

    public static App OnExecuteAction(this App app, Func<IContext<ExecuteActionActivity>, CancellationToken, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.ExecuteAction]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<ExecuteActionActivity>(), context.CancellationToken);
                return null;
            },
            Selector = activity => activity is ExecuteActionActivity
        });

        return app;
    }

    public static App OnExecuteAction(this App app, Func<IContext<ExecuteActionActivity>, CancellationToken, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.ExecuteAction]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = context => handler(context.ToActivityType<ExecuteActionActivity>(), context.CancellationToken),
            Selector = activity => activity is ExecuteActionActivity
        });

        return app;
    }
}