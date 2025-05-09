using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class MessageDeleteAttribute() : ActivityAttribute(ActivityType.MessageDelete, typeof(MessageDeleteActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageDeleteActivity>();
}

public static partial class AppActivityExtensions
{
    public static App OnMessageDelete(this App app, Func<IContext<MessageDeleteActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageDeleteActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is MessageDeleteActivity messageDelete)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}