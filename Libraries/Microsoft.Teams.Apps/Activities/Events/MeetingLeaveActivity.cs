using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Events;

public static partial class Event
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class MeetingLeaveAttribute() : EventAttribute
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MeetingParticipantLeaveActivity>();
        public override bool Select(IActivity activity)
        {
            if (activity is MeetingParticipantLeaveActivity)
            {
                return true;
            }

            return false;
        }
    }
}

public static partial class AppEventActivityExtensions
{
    public static App OnMeetingLeave(this App app, Func<IContext<MeetingParticipantLeaveActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<MeetingParticipantLeaveActivity>()),
            Selector = activity =>
            {
                if (activity is MeetingParticipantLeaveActivity)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}