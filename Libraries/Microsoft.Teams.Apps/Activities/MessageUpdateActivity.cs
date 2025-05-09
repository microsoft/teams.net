using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class MessageUpdateAttribute() : ActivityAttribute(ActivityType.MessageUpdate, typeof(MessageUpdateActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageUpdateActivity>();
}

public static partial class AppExtensions
{
    public static App OnMessageUpdate(this App app, Func<IContext<MessageUpdateActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageUpdateActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is MessageUpdateActivity messageUpdate)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}