// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class VerifyStateAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.SignIn.VerifyState, typeof(SignIn.VerifyStateActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<SignIn.VerifyStateActivity>();
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnVerifyState(this App app, Func<IContext<SignIn.VerifyStateActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SignIn.VerifyState]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<SignIn.VerifyStateActivity>());
                return null;
            },
            Selector = activity => activity is SignIn.VerifyStateActivity
        });

        return app;
    }

    public static App OnVerifyState(this App app, Func<IContext<SignIn.VerifyStateActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SignIn.VerifyState]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = context => handler(context.ToActivityType<SignIn.VerifyStateActivity>()),
            Selector = activity => activity is SignIn.VerifyStateActivity
        });

        return app;
    }

    public static App OnVerifyState(this App app, Func<IContext<SignIn.VerifyStateActivity>, Task<Response?>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SignIn.VerifyState]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<SignIn.VerifyStateActivity>()),
            Selector = activity => activity is SignIn.VerifyStateActivity
        });

        return app;
    }

    public static App OnVerifyState(this App app, Func<IContext<SignIn.VerifyStateActivity>, CancellationToken, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SignIn.VerifyState]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<SignIn.VerifyStateActivity>(), context.CancellationToken);
                return null;
            },
            Selector = activity => activity is SignIn.VerifyStateActivity
        });

        return app;
    }

    public static App OnVerifyState(this App app, Func<IContext<SignIn.VerifyStateActivity>, CancellationToken, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SignIn.VerifyState]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = context => handler(context.ToActivityType<SignIn.VerifyStateActivity>(), context.CancellationToken),
            Selector = activity => activity is SignIn.VerifyStateActivity
        });

        return app;
    }

    public static App OnVerifyState(this App app, Func<IContext<SignIn.VerifyStateActivity>, CancellationToken, Task<Response?>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SignIn.VerifyState]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<SignIn.VerifyStateActivity>(), context.CancellationToken),
            Selector = activity => activity is SignIn.VerifyStateActivity
        });

        return app;
    }
}