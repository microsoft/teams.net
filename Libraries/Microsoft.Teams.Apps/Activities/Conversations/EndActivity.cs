using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

public static partial class Conversation
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class EndAttribute() : ActivityAttribute(ActivityType.EndOfConversation, typeof(EndOfConversationActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<EndOfConversationActivity>();
    }
}

public static partial class AppActivityExtensions
{
    public static App OnConversationEnd(this App app, Func<IContext<EndOfConversationActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<EndOfConversationActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is EndOfConversationActivity endOfConversation)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}