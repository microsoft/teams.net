// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class MessageExtension
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class AnonQueryLinkAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.MessageExtensions.AnonQueryLink, typeof(MessageExtensions.AnonQueryLinkActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageExtensions.AnonQueryLinkActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnAnonQueryLink(this App app, Func<IContext<MessageExtensions.AnonQueryLinkActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.MessageExtensions.AnonQueryLink]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageExtensions.AnonQueryLinkActivity>());
                return null;
            },
            Selector = activity => activity is MessageExtensions.AnonQueryLinkActivity
        });

        return app;
    }

    public static App OnAnonQueryLink(this App app, Func<IContext<MessageExtensions.AnonQueryLinkActivity>, Task<Response<Api.MessageExtensions.Response>>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.MessageExtensions.AnonQueryLink]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<MessageExtensions.AnonQueryLinkActivity>()),
            Selector = activity => activity is MessageExtensions.AnonQueryLinkActivity
        });

        return app;
    }

    public static App OnAnonQueryLink(this App app, Func<IContext<MessageExtensions.AnonQueryLinkActivity>, Task<Api.MessageExtensions.Response>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.MessageExtensions.AnonQueryLink]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<MessageExtensions.AnonQueryLinkActivity>()),
            Selector = activity => activity is MessageExtensions.AnonQueryLinkActivity
        });

        return app;
    }
}