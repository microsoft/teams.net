// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

public static partial class Conversation
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class ChannelCreatedAttribute() : UpdateAttribute
    {
        public override bool Select(IActivity activity)
        {
            if (activity is ConversationUpdateActivity update)
            {
                return update.ChannelData?.EventType == ConversationUpdateActivity.EventType.ChannelCreated;
            }

            return false;
        }
    }
}

public static partial class AppActivityExtensions
{
    public static App OnChannelCreated(this App app, Func<IContext<ConversationUpdateActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<ConversationUpdateActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is ConversationUpdateActivity update)
                {
                    return update.ChannelData?.EventType == ConversationUpdateActivity.EventType.ChannelCreated;
                }

                return false;
            }
        });

        return app;
    }
}