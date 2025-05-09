using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class MessageReactionAttribute() : ActivityAttribute(ActivityType.MessageReaction, typeof(MessageReactionActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageReactionActivity>();
}

public static partial class AppExtensions
{
    public static App OnMessageReaction(this App app, Func<IContext<MessageReactionActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageReactionActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is MessageReactionActivity messageReaction)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}