// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

public static partial class Conversation
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class TeamDeletedAttribute(bool hard = false) : UpdateAttribute(hard ? ConversationUpdateActivity.EventType.TeamHardDeleted : ConversationUpdateActivity.EventType.TeamDeleted)
    {
        public override bool Select(IActivity activity)
        {
            if (activity is ConversationUpdateActivity update)
            {
                return !hard
                    ? update.ChannelData?.EventType == ConversationUpdateActivity.EventType.TeamDeleted
                    : update.ChannelData?.EventType == ConversationUpdateActivity.EventType.TeamHardDeleted;
            }

            return false;
        }
    }
}

public static partial class AppActivityExtensions
{
    public static App OnTeamDeleted(this App app, Func<IContext<ConversationUpdateActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.ConversationUpdate, ConversationUpdateActivity.EventType.TeamDeleted]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<ConversationUpdateActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is ConversationUpdateActivity update)
                {
                    return update.ChannelData?.EventType == ConversationUpdateActivity.EventType.TeamDeleted;
                }

                return false;
            }
        });

        return app;
    }

    public static App OnTeamHardDeleted(this App app, Func<IContext<ConversationUpdateActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.ConversationUpdate, ConversationUpdateActivity.EventType.TeamHardDeleted]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<ConversationUpdateActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is ConversationUpdateActivity update)
                {
                    return update.ChannelData?.EventType == ConversationUpdateActivity.EventType.TeamHardDeleted;
                }

                return false;
            }
        });

        return app;
    }
}