// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class TaskSubmitAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.Tasks.Submit, typeof(Tasks.SubmitActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<Tasks.SubmitActivity>();
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnTaskSubmit(this App app, Func<IContext<Tasks.SubmitActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Tasks.Submit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<Tasks.SubmitActivity>());
                return null;
            },
            Selector = activity => activity is Tasks.SubmitActivity
        });

        return app;
    }

    public static App OnTaskSubmit(this App app, Func<IContext<Tasks.SubmitActivity>, Task<Response<Api.TaskModules.Response>>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Tasks.Submit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<Tasks.SubmitActivity>()),
            Selector = activity => activity is Tasks.SubmitActivity
        });

        return app;
    }

    public static App OnTaskSubmit(this App app, Func<IContext<Tasks.SubmitActivity>, Task<Api.TaskModules.Response>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Tasks.Submit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<Tasks.SubmitActivity>()),
            Selector = activity => activity is Tasks.SubmitActivity
        });

        return app;
    }

    public static App OnTaskSubmit(this App app, Func<IContext<Tasks.SubmitActivity>, CancellationToken, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Tasks.Submit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<Tasks.SubmitActivity>(), context.CancellationToken);
                return null;
            },
            Selector = activity => activity is Tasks.SubmitActivity
        });

        return app;
    }

    public static App OnTaskSubmit(this App app, Func<IContext<Tasks.SubmitActivity>, CancellationToken, Task<Response<Api.TaskModules.Response>>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Tasks.Submit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<Tasks.SubmitActivity>(), context.CancellationToken),
            Selector = activity => activity is Tasks.SubmitActivity
        });

        return app;
    }

    public static App OnTaskSubmit(this App app, Func<IContext<Tasks.SubmitActivity>, CancellationToken, Task<Api.TaskModules.Response>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Tasks.Submit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<Tasks.SubmitActivity>(), context.CancellationToken),
            Selector = activity => activity is Tasks.SubmitActivity
        });

        return app;
    }
}