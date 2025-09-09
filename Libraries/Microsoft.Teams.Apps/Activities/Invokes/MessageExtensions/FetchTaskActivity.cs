// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class MessageExtension
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class FetchTaskAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.MessageExtensions.FetchTask, typeof(MessageExtensions.FetchTaskActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageExtensions.FetchTaskActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnFetchTask(this App app, Func<IContext<MessageExtensions.FetchTaskActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.MessageExtensions.FetchTask]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageExtensions.FetchTaskActivity>());
                return null;
            },
            Selector = activity => activity is MessageExtensions.FetchTaskActivity
        });

        return app;
    }

    public static App OnFetchTask(this App app, Func<IContext<MessageExtensions.FetchTaskActivity>, Task<Response<Api.MessageExtensions.ActionResponse>>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.MessageExtensions.FetchTask]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<MessageExtensions.FetchTaskActivity>()),
            Selector = activity => activity is MessageExtensions.FetchTaskActivity
        });

        return app;
    }

    public static App OnFetchTask(this App app, Func<IContext<MessageExtensions.FetchTaskActivity>, Task<Api.MessageExtensions.ActionResponse>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.MessageExtensions.FetchTask]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<MessageExtensions.FetchTaskActivity>()),
            Selector = activity => activity is MessageExtensions.FetchTaskActivity
        });

        return app;
    }
}