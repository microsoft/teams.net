// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

public static partial class Message
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class UpdateAttribute() : ActivityAttribute(ActivityType.MessageUpdate, typeof(MessageUpdateActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageUpdateActivity>();
    }
}

public static partial class AppActivityExtensions
{
    public static App OnMessageUpdate(this App app, Func<IContext<MessageUpdateActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.MessageUpdate,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageUpdateActivity>());
                return null;
            },
            Selector = activity => activity is MessageUpdateActivity
        });

        return app;
    }
}