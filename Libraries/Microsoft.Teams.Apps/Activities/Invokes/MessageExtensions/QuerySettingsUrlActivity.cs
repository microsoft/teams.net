// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class MessageExtension
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class QuerySettingsUrlAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.MessageExtensions.QuerySettingsUrl, typeof(MessageExtensions.QuerySettingsUrlActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageExtensions.QuerySettingsUrlActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnQuerySettingsUrl(this App app, Func<IContext<MessageExtensions.QuerySettingsUrlActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.MessageExtensions.QuerySettingsUrl]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageExtensions.QuerySettingsUrlActivity>());
                return null;
            },
            Selector = activity => activity is MessageExtensions.QuerySettingsUrlActivity
        });

        return app;
    }

    public static App OnQuerySettingsUrl(this App app, Func<IContext<MessageExtensions.QuerySettingsUrlActivity>, Task<Response<Api.MessageExtensions.Response>>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.MessageExtensions.QuerySettingsUrl]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<MessageExtensions.QuerySettingsUrlActivity>()),
            Selector = activity => activity is MessageExtensions.QuerySettingsUrlActivity
        });

        return app;
    }

    public static App OnQuerySettingsUrl(this App app, Func<IContext<MessageExtensions.QuerySettingsUrlActivity>, Task<Api.MessageExtensions.Response>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.MessageExtensions.QuerySettingsUrl]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<MessageExtensions.QuerySettingsUrlActivity>()),
            Selector = activity => activity is MessageExtensions.QuerySettingsUrlActivity
        });

        return app;
    }
}