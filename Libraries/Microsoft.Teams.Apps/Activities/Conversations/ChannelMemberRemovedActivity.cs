using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

public static partial class Conversation
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class ChannelMemberRemovedAttribute() : UpdateAttribute(ConversationUpdateActivity.EventType.ChannelMemberRemoved)
    {
        public override bool Select(IActivity activity)
        {
            if (activity is ConversationUpdateActivity update)
            {
                return update.ChannelData?.EventType == ConversationUpdateActivity.EventType.ChannelMemberRemoved;
            }

            return false;
        }
    }
}

public static partial class AppActivityExtensions
{
    public static App OnChannelMemberRemoved(this App app, Func<IContext<ConversationUpdateActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.ConversationUpdate, ConversationUpdateActivity.EventType.ChannelMemberRemoved]),
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
                    return update.ChannelData?.EventType == ConversationUpdateActivity.EventType.ChannelMemberRemoved;
                }

                return false;
            }
        });

        return app;
    }
}