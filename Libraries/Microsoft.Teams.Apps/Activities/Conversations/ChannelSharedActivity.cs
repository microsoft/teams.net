using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

public static partial class Conversation
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class ChannelSharedAttribute() : UpdateAttribute(ConversationUpdateActivity.EventType.ChannelShared)
    {
        public override bool Select(IActivity activity)
        {
            if (activity is ConversationUpdateActivity update)
            {
                return update.ChannelData?.EventType == ConversationUpdateActivity.EventType.ChannelShared;
            }

            return false;
        }
    }
}

public static partial class AppActivityExtensions
{
    public static App OnChannelShared(this App app, Func<IContext<ConversationUpdateActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.ConversationUpdate, ConversationUpdateActivity.EventType.ChannelShared]),
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
                    return update.ChannelData?.EventType == ConversationUpdateActivity.EventType.ChannelShared;
                }

                return false;
            }
        });

        return app;
    }

    public static App OnChannelShared(this App app, Func<IContext<ConversationUpdateActivity>, CancellationToken, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.ConversationUpdate, ConversationUpdateActivity.EventType.ChannelShared]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<ConversationUpdateActivity>(), context.CancellationToken);
                return null;
            },
            Selector = activity =>
            {
                if (activity is ConversationUpdateActivity update)
                {
                    return update.ChannelData?.EventType == ConversationUpdateActivity.EventType.ChannelShared;
                }

                return false;
            }
        });

        return app;
    }
}