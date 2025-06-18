using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Events;

public static partial class Event
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class MeetingStartAttribute() : EventAttribute
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MeetingStartActivity>();
        public override bool Select(IActivity activity)
        {
            if (activity is MeetingStartActivity)
            {
                return true;
            }

            return false;
        }
    }
}

public static partial class AppEventActivityExtensions
{
    public static App OnMeetingStart(this App app, Func<IContext<MeetingStartActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<MeetingStartActivity>());
                return null;
            },
            Selector = activity => activity is MeetingStartActivity
        });

        return app;
    }
}