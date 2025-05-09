using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class TypingAttribute() : ActivityAttribute(ActivityType.Typing, typeof(TypingActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<TypingActivity>();
}

public static partial class AppExtensions
{
    public static App OnTyping(this App app, Func<IContext<TypingActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<TypingActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is TypingActivity typing)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}