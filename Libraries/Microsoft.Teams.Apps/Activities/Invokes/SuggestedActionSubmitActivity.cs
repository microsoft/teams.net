// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class SuggestedActionSubmitAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.SuggestedActionSubmit, typeof(SuggestedActionSubmitActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<SuggestedActionSubmitActivity>();
}

public static partial class AppInvokeActivityExtensions
{
    [Obsolete("Use the handler with the cancellation token")]
    public static App OnSuggestedActionSubmit(this App app, Func<IContext<SuggestedActionSubmitActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SuggestedActionSubmit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<SuggestedActionSubmitActivity>());
                return null;
            },
            Selector = activity => activity is SuggestedActionSubmitActivity
        });

        return app;
    }

    [Obsolete("Use the handler with the cancellation token")]
    public static App OnSuggestedActionSubmit(this App app, Func<IContext<SuggestedActionSubmitActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SuggestedActionSubmit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = context => handler(context.ToActivityType<SuggestedActionSubmitActivity>()),
            Selector = activity => activity is SuggestedActionSubmitActivity
        });

        return app;
    }

    public static App OnSuggestedActionSubmit(this App app, Func<IContext<SuggestedActionSubmitActivity>, CancellationToken, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SuggestedActionSubmit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<SuggestedActionSubmitActivity>(), context.CancellationToken);
                return null;
            },
            Selector = activity => activity is SuggestedActionSubmitActivity
        });

        return app;
    }

    public static App OnSuggestedActionSubmit(this App app, Func<IContext<SuggestedActionSubmitActivity>, CancellationToken, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.SuggestedActionSubmit]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = context => handler(context.ToActivityType<SuggestedActionSubmitActivity>(), context.CancellationToken),
            Selector = activity => activity is SuggestedActionSubmitActivity
        });

        return app;
    }
}
