// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

public static partial class Conversation
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    [Obsolete("This will be removed by end of summer 2026.")]
    #pragma warning disable CS0618
    public class EndAttribute() : ActivityAttribute(ActivityType.EndOfConversation, typeof(EndOfConversationActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<EndOfConversationActivity>();
    }
    #pragma warning restore CS0618
}

public static partial class AppActivityExtensions
{
    [Obsolete("Use the handler with the cancellation token. This will be removed by end of summer 2026.")]
    #pragma warning disable CS0618
    public static App OnConversationEnd(this App app, Func<IContext<EndOfConversationActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.EndOfConversation,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<EndOfConversationActivity>()).ConfigureAwait(false);
                return null;
            },
            Selector = activity => activity is EndOfConversationActivity
        });

        return app;
    }
    #pragma warning restore CS0618

    [Obsolete("This will be removed by end of summer 2026.")]
    #pragma warning disable CS0618
    public static App OnConversationEnd(this App app, Func<IContext<EndOfConversationActivity>, CancellationToken, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.EndOfConversation,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<EndOfConversationActivity>(), context.CancellationToken).ConfigureAwait(false);
                return null;
            },
            Selector = activity => activity is EndOfConversationActivity
        });

        return app;
    }
    #pragma warning restore CS0618
}