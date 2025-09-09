// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class TokenExchangeAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.SignIn.TokenExchange, typeof(SignIn.TokenExchangeActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<SignIn.TokenExchangeActivity>();
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnTokenExchange(this App app, Func<IContext<SignIn.TokenExchangeActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SignIn.TokenExchange]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<SignIn.TokenExchangeActivity>());
                return null;
            },
            Selector = activity => activity is SignIn.TokenExchangeActivity
        });

        return app;
    }

    public static App OnTokenExchange(this App app, Func<IContext<SignIn.TokenExchangeActivity>, Task<Response<Api.TokenExchange.InvokeResponse>>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SignIn.TokenExchange]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<SignIn.TokenExchangeActivity>()),
            Selector = activity => activity is SignIn.TokenExchangeActivity
        });

        return app;
    }

    public static App OnTokenExchange(this App app, Func<IContext<SignIn.TokenExchangeActivity>, Task<Api.TokenExchange.InvokeResponse>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SignIn.TokenExchange]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<SignIn.TokenExchangeActivity>()),
            Selector = activity => activity is SignIn.TokenExchangeActivity
        });

        return app;
    }

    public static App OnTokenExchange(this App app, Func<IContext<SignIn.TokenExchangeActivity>, Task<Response>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SignIn.TokenExchange]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<SignIn.TokenExchangeActivity>()),
            Selector = activity => activity is SignIn.TokenExchangeActivity
        });

        return app;
    }
}