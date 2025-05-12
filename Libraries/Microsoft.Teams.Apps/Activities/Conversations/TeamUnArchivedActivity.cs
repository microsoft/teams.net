using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

public static partial class Conversation
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class TeamUnArchivedAttribute() : UpdateAttribute
    {
        public override bool Select(IActivity activity)
        {
            if (activity is ConversationUpdateActivity update)
            {
                return update.ChannelData?.EventType == ConversationUpdateActivity.EventType.TeamUnarchived;
            }

            return false;
        }
    }
}

public static partial class AppActivityExtensions
{
    public static App OnTeamUnArchived(this App app, Func<IContext<ConversationUpdateActivity>, Task> handler)
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
                    return update.ChannelData?.EventType == ConversationUpdateActivity.EventType.TeamUnarchived;
                }

                return false;
            }
        });

        return app;
    }
}
