// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

public static partial class Message
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class DeleteAttribute() : ActivityAttribute(ActivityType.MessageDelete, typeof(MessageDeleteActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageDeleteActivity>();
    }
}

public static partial class AppActivityExtensions
{
    public static App OnMessageDelete(this App app, Func<IContext<MessageDeleteActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.MessageDelete,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageDeleteActivity>());
                return null;
            },
            Selector = activity => activity is MessageDeleteActivity
        });

        return app;
    }

    public static App OnMessageDelete(this App app, Func<IContext<MessageDeleteActivity>, CancellationToken, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.MessageDelete,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageDeleteActivity>(), context.CancellationToken);
                return null;
            },
            Selector = activity => activity is MessageDeleteActivity
        });

        return app;
    }
}