
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Apps.Annotations;

using static Microsoft.Teams.Apps.Activities.Events.Event;

namespace Microsoft.Teams.Apps.Tests.Activities.Events;

[TeamsController]
public class MeetingActivityController
{
    public string MethodCalled { get; set; } = string.Empty;

    [MeetingStart]
    public async Task Method1(IContext<MeetingStartActivity> context, [Context] IContext.Next next)
    {
        MethodCalled = "meetingStartMethod";
        await next();
    }


    [MeetingEnd]
    public async Task Method2(IContext<MeetingEndActivity> context, [Context] IContext.Next next)
    {
        MethodCalled = "meetingEndMethod";
        await next();
    }


    [MeetingJoin]
    public async Task Method3(IContext<MeetingParticipantJoinActivity> context, [Context] IContext.Next next)
    {
        MethodCalled = "meetingJoinMethod";
        await next();
    }


    [MeetingLeave]
    public async Task Method4(IContext<MeetingParticipantLeaveActivity> context, [Context] IContext.Next next)
    {
        MethodCalled = "meetingLeaveMethod";
        await next();
    }
}