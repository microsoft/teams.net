// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.Config;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class Config
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class SubmitAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.Configs.Submit, typeof(Configs.SubmitActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<Configs.SubmitActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnConfigSubmit(this App app, Func<IContext<Configs.SubmitActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Configs.Submit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<Configs.SubmitActivity>());
                return null;
            },
            Selector = activity => activity is Configs.SubmitActivity
        });

        return app;
    }

    public static App OnConfigSubmit(this App app, Func<IContext<Configs.SubmitActivity>, Task<Response<ConfigResponse>>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Configs.Submit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<Configs.SubmitActivity>()),
            Selector = activity => activity is Configs.SubmitActivity
        });

        return app;
    }

    public static App OnConfigSubmit(this App app, Func<IContext<Configs.SubmitActivity>, Task<ConfigResponse>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Configs.Submit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<Configs.SubmitActivity>()),
            Selector = activity => activity is Configs.SubmitActivity
        });

        return app;
    }

    public static App OnConfigSubmit(this App app, Func<IContext<Configs.SubmitActivity>, CancellationToken, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Configs.Submit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<Configs.SubmitActivity>(), context.CancellationToken);
                return null;
            },
            Selector = activity => activity is Configs.SubmitActivity
        });

        return app;
    }

    public static App OnConfigSubmit(this App app, Func<IContext<Configs.SubmitActivity>, CancellationToken, Task<Response<ConfigResponse>>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Configs.Submit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<Configs.SubmitActivity>(), context.CancellationToken),
            Selector = activity => activity is Configs.SubmitActivity
        });

        return app;
    }

    public static App OnConfigSubmit(this App app, Func<IContext<Configs.SubmitActivity>, CancellationToken, Task<ConfigResponse>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Configs.Submit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<Configs.SubmitActivity>(), context.CancellationToken),
            Selector = activity => activity is Configs.SubmitActivity
        });

        return app;
    }
}