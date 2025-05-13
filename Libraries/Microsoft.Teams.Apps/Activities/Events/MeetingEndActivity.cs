using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Events;

public static partial class Event
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class MeetingEndAttribute() : EventAttribute
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MeetingEndActivity>();
        public override bool Select(IActivity activity)
        {
            if (activity is MeetingEndActivity)
            {
                return true;
            }

            return false;
        }
    }
}

public static partial class AppEventActivityExtensions
{
    public static App OnMeetingEnd(this App app, Func<IContext<MeetingEndActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<MeetingEndActivity>()),
            Selector = activity => activity is MeetingEndActivity
        });

        return app;
    }
}