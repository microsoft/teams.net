using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class ConversationUpdateAttribute() : ActivityAttribute(ActivityType.ConversationUpdate, typeof(ConversationUpdateActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<ConversationUpdateActivity>();
}

public static partial class AppActivityExtensions
{
    public static App OnConversationUpdate(this App app, Func<IContext<ConversationUpdateActivity>, Task> handler)
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
                if (activity is ConversationUpdateActivity conversationUpdate)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}