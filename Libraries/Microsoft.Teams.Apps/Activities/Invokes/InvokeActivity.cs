// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class InvokeAttribute : ActivityAttribute
{
    public Name? InvokeName { get; }

    public InvokeAttribute(string? name = null, Type? type = null) : base(name is null ? ActivityType.Invoke : string.Join("/", [ActivityType.Invoke, name]), type ?? typeof(InvokeActivity))
    {
        InvokeName = name is not null ? new(name) : null;
    }

    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<InvokeActivity>();
    public override bool Select(IActivity activity)
    {
        if (activity is InvokeActivity invoke)
        {
            return invoke.Name.Equals(InvokeName);
        }

        return false;
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnInvoke(this App app, Func<IContext<InvokeActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.Invoke,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<InvokeActivity>());
                return null;
            },
            Selector = activity => activity is InvokeActivity
        });

        return app;
    }

    public static App OnInvoke(this App app, Func<IContext<InvokeActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.Invoke,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = context => handler(context.ToActivityType<InvokeActivity>()),
            Selector = activity => activity is InvokeActivity
        });

        return app;
    }

    public static App OnInvoke(this App app, Func<IContext<InvokeActivity>, Task<Response>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.Invoke,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<InvokeActivity>()),
            Selector = activity => activity is InvokeActivity
        });

        return app;
    }

    public static App OnInvoke(this App app, Func<IContext<InvokeActivity>, CancellationToken, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.Invoke,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<InvokeActivity>(), context.CancellationToken);
                return null;
            },
            Selector = activity => activity is InvokeActivity
        });

        return app;
    }

    public static App OnInvoke(this App app, Func<IContext<InvokeActivity>, CancellationToken, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.Invoke,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = context => handler(context.ToActivityType<InvokeActivity>(), context.CancellationToken),
            Selector = activity => activity is InvokeActivity
        });

        return app;
    }

    public static App OnInvoke(this App app, Func<IContext<InvokeActivity>, CancellationToken, Task<Response>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.Invoke,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<InvokeActivity>(), context.CancellationToken),
            Selector = activity => activity is InvokeActivity
        });

        return app;
    }
}