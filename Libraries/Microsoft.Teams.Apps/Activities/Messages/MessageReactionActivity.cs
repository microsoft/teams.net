// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

public static partial class Message
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class ReactionAttribute() : ActivityAttribute(ActivityType.MessageReaction, typeof(MessageReactionActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageReactionActivity>();
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class ReactionAddedAttribute() : ReactionAttribute
    {
        public override bool Select(IActivity activity)
        {
            if (activity is MessageReactionActivity messageReaction)
            {
                return messageReaction.ReactionsAdded?.Count > 0;
            }

            return false;
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class ReactionRemovedAttribute() : ReactionAttribute
    {
        public override bool Select(IActivity activity)
        {
            if (activity is MessageReactionActivity messageReaction)
            {
                return messageReaction.ReactionsRemoved?.Count > 0;
            }

            return false;
        }
    }
}

public static partial class AppActivityExtensions
{
    public static App OnMessageReaction(this App app, Func<IContext<MessageReactionActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.MessageReaction,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageReactionActivity>());
                return null;
            },
            Selector = activity => activity is MessageReactionActivity
        });

        return app;
    }

    public static App OnMessageReactionAdded(this App app, Func<IContext<MessageReactionActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.MessageReaction,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageReactionActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is MessageReactionActivity messageReaction)
                {
                    return messageReaction.ReactionsAdded?.Count > 0;
                }

                return false;
            }
        });

        return app;
    }

    public static App OnMessageReactionRemoved(this App app, Func<IContext<MessageReactionActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.MessageReaction,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageReactionActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is MessageReactionActivity messageReaction)
                {
                    return messageReaction.ReactionsRemoved?.Count > 0;
                }

                return false;
            }
        });

        return app;
    }
}