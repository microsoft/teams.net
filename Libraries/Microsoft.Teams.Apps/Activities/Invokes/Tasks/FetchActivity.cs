// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class TaskFetchAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.Tasks.Fetch, typeof(Tasks.FetchActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<Tasks.FetchActivity>();
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnTaskFetch(this App app, Func<IContext<Tasks.FetchActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Tasks.Fetch]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<Tasks.FetchActivity>());
                return null;
            },
            Selector = activity => activity is Tasks.FetchActivity
        });

        return app;
    }

    public static App OnTaskFetch(this App app, Func<IContext<Tasks.FetchActivity>, Task<Response<Api.TaskModules.Response>>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Tasks.Fetch]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<Tasks.FetchActivity>()),
            Selector = activity => activity is Tasks.FetchActivity
        });

        return app;
    }

    public static App OnTaskFetch(this App app, Func<IContext<Tasks.FetchActivity>, Task<Api.TaskModules.Response>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Tasks.Fetch]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<Tasks.FetchActivity>()),
            Selector = activity => activity is Tasks.FetchActivity
        });

        return app;
    }

    public static App OnTaskFetch(this App app, Func<IContext<Tasks.FetchActivity>, CancellationToken, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Tasks.Fetch]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<Tasks.FetchActivity>(), context.CancellationToken);
                return null;
            },
            Selector = activity => activity is Tasks.FetchActivity
        });

        return app;
    }

    public static App OnTaskFetch(this App app, Func<IContext<Tasks.FetchActivity>, CancellationToken, Task<Response<Api.TaskModules.Response>>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Tasks.Fetch]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<Tasks.FetchActivity>(), context.CancellationToken),
            Selector = activity => activity is Tasks.FetchActivity
        });

        return app;
    }

    public static App OnTaskFetch(this App app, Func<IContext<Tasks.FetchActivity>, CancellationToken, Task<Api.TaskModules.Response>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Tasks.Fetch]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<Tasks.FetchActivity>(), context.CancellationToken),
            Selector = activity => activity is Tasks.FetchActivity
        });

        return app;
    }
}