// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Events;

public static partial class Event
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class MeetingJoinAttribute() : EventAttribute
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MeetingParticipantJoinActivity>();
        public override bool Select(IActivity activity)
        {
            if (activity is MeetingParticipantJoinActivity)
            {
                return true;
            }

            return false;
        }
    }
}

public static partial class AppEventActivityExtensions
{
    public static App OnMeetingJoin(this App app, Func<IContext<MeetingParticipantJoinActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<MeetingParticipantJoinActivity>());
                return null;
            },
            Selector = activity => activity is MeetingParticipantJoinActivity
        });

        return app;
    }
}