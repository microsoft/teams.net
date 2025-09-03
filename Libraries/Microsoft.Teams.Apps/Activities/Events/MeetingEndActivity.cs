// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Events;

public static partial class Event
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class MeetingEndAttribute() : EventAttribute(Api.Activities.Events.Name.MeetingEnd)
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
    public static App OnMeetingEnd(this App app, Func<IContext<MeetingEndActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Event, Name.MeetingEnd]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<MeetingEndActivity>());
                return null;
            },
            Selector = activity => activity is MeetingEndActivity
        });

        return app;
    }
}