using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class EventAttribute() : ActivityAttribute(ActivityType.Event, typeof(EventActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<EventActivity>();
}

public static partial class AppExtensions
{
    public static App OnEvent(this App app, Func<IContext<EventActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<EventActivity>()),
            Selector = activity =>
            {
                if (activity is EventActivity @event)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}