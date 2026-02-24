// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

/// <summary>
/// Attribute for handling signin/failure invoke activities sent when SSO token exchange fails.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class FailureAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.SignIn.Failure, typeof(SignIn.FailureActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<SignIn.FailureActivity>();
}

public static partial class AppInvokeActivityExtensions
{
    /// <summary>
    /// Registers a handler for signin/failure invoke activities sent when SSO token exchange fails.
    /// </summary>
    public static App OnFailure(this App app, Func<IContext<SignIn.FailureActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SignIn.Failure]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<SignIn.FailureActivity>());
                return null;
            },
            Selector = activity => activity is SignIn.FailureActivity
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for signin/failure invoke activities sent when SSO token exchange fails.
    /// </summary>
    public static App OnFailure(this App app, Func<IContext<SignIn.FailureActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SignIn.Failure]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = context => handler(context.ToActivityType<SignIn.FailureActivity>()),
            Selector = activity => activity is SignIn.FailureActivity
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for signin/failure invoke activities sent when SSO token exchange fails.
    /// </summary>
    public static App OnFailure(this App app, Func<IContext<SignIn.FailureActivity>, Task<Response?>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SignIn.Failure]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<SignIn.FailureActivity>()),
            Selector = activity => activity is SignIn.FailureActivity
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for signin/failure invoke activities sent when SSO token exchange fails, with cancellation token support.
    /// </summary>
    public static App OnFailure(this App app, Func<IContext<SignIn.FailureActivity>, CancellationToken, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SignIn.Failure]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<SignIn.FailureActivity>(), context.CancellationToken);
                return null;
            },
            Selector = activity => activity is SignIn.FailureActivity
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for signin/failure invoke activities sent when SSO token exchange fails, with cancellation token support.
    /// </summary>
    public static App OnFailure(this App app, Func<IContext<SignIn.FailureActivity>, CancellationToken, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SignIn.Failure]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = context => handler(context.ToActivityType<SignIn.FailureActivity>(), context.CancellationToken),
            Selector = activity => activity is SignIn.FailureActivity
        });

        return app;
    }

    /// <summary>
    /// Registers a handler for signin/failure invoke activities sent when SSO token exchange fails, with cancellation token support.
    /// </summary>
    public static App OnFailure(this App app, Func<IContext<SignIn.FailureActivity>, CancellationToken, Task<Response?>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SignIn.Failure]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<SignIn.FailureActivity>(), context.CancellationToken),
            Selector = activity => activity is SignIn.FailureActivity
        });

        return app;
    }
}
