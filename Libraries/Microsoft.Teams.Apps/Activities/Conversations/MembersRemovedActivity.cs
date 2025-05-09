using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

public static partial class Conversation
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class MembersRemovedAttribute() : UpdateAttribute
    {
        public override bool Select(IActivity activity)
        {
            if (activity is ConversationUpdateActivity update)
            {
                return update.MembersRemoved.Length > 0;
            }

            return false;
        }
    }
}

public static partial class AppActivityExtensions
{
    public static App OnMembersRemoved(this App app, Func<IContext<ConversationUpdateActivity>, Task> handler)
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
                    return update.MembersRemoved.Length > 0;
                }

                return false;
            }
        });

        return app;
    }
}
