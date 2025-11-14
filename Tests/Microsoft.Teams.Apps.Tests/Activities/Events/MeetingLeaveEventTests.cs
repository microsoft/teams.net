
using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities.Events;
using Microsoft.Teams.Apps.Testing.Plugins;

using Microsoft.Extensions.Logging.Abstractions;

using static Microsoft.Teams.Apps.Activities.Events.Event;

namespace Microsoft.Teams.Apps.Tests.Activities.Events;

public class MeetingLeaveEventTests
{
    private readonly App _app;
    private readonly TestPlugin _plugin = new();
    private readonly MeetingActivityController _controller = new();
    private readonly IToken _token = Globals.Token;

    public MeetingLeaveEventTests()
    {
        _app = new App(NullLogger<App>.Instance);
        _app.AddPlugin(_plugin);
        _app.AddController(_controller);
        _token = Globals.Token;
    }

    [Fact]
    public async Task Should_CallHandler_OnMeetingLeaveEvent()
    {
        // Arrange
        var handlerCalled = false;
        var eventContext = default(IContext<MeetingParticipantLeaveActivity>);

        _app.OnMeetingLeave(context =>
        {
            handlerCalled = true;
            eventContext = context;
            return Task.FromResult<object?>(null);
        });

        // Create a MeetingLeaveActivity
        var meetingLeaveActivity = new MeetingParticipantLeaveActivity
        {
            Value = new MeetingParticipantLeaveActivityValue
            {
                Members = new List<MeetingParticipantLeaveActivityValue.Member>
                {
                    new MeetingParticipantLeaveActivityValue.Member
                    {
                        User = new Account { Id = "user1", Name = "Test User" },
                        Meeting = new MeetingParticipantLeaveActivityValue.Meeting
                        {
                            InMeeting = true,
                            Role = Role.User
                        }
                    },

                }
            }
        };

        // Act
        var res = await _plugin.Do(_token, meetingLeaveActivity);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.True(handlerCalled, "The MeetingLeave event handler should be called");
        Assert.NotNull(eventContext);
        Assert.IsType<MeetingParticipantLeaveActivity>(eventContext.Activity);
        Assert.Single(eventContext.Activity.Value.Members);
    }

    [Fact]
    public async Task Should_NotCallHandler_ForOtherEventTypes()
    {
        // Arrange
        var handlerCalled = false;

        _app.OnMeetingLeave(context =>
        {
            handlerCalled = true;
            return Task.FromResult<object?>(null);
        });

        // Act - Leave a different activity type
        var res = await _plugin.Do(_token, new ReadReceiptActivity());

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.False(handlerCalled, "The MeetingLeave event handler should not be called for other activity types");
    }

    [Fact]
    public void MeetingLeaveAttribute_Select_ReturnsTrueForMeetingLeaveActivity()
    {
        // Arrange
        var attribute = new MeetingLeaveAttribute();
        var activity = new MeetingParticipantLeaveActivity
        {
            Value = new MeetingParticipantLeaveActivityValue
            {
                Members = new List<MeetingParticipantLeaveActivityValue.Member>
                {
                    new MeetingParticipantLeaveActivityValue.Member
                    {
                        User = new Account { Id = "user1", Name = "Test User" },
                        Meeting = new MeetingParticipantLeaveActivityValue.Meeting
                        {
                            InMeeting = true,
                            Role = Role.User
                        }
                    },

                }
            }
        };

        // Act
        var result = attribute.Select(activity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MeetingLeaveAttribute_Select_ReturnsFalseForOtherActivities()
    {
        // Arrange
        var attribute = new MeetingLeaveAttribute();
        var activity = new MessageActivity("hello world");

        // Act
        var result = attribute.Select(activity);

        // Assert
        Assert.False(result);
    }


    [Fact]
    public async Task MeetingLeaveAttribute_Controller_Call()
    {
        var activity = new MeetingParticipantLeaveActivity
        {
            Value = new MeetingParticipantLeaveActivityValue
            {
                Members = new List<MeetingParticipantLeaveActivityValue.Member>
                {
                    new MeetingParticipantLeaveActivityValue.Member
                    {
                        User = new Account { Id = "user1", Name = "Test User" },
                        Meeting = new MeetingParticipantLeaveActivityValue.Meeting
                        {
                            InMeeting = true,
                            Role = Role.User
                        }
                    },

                }
            }
        };

        var res = await _app.Process<TestPlugin>(_token, activity);

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal("meetingLeaveMethod", _controller.MethodCalled);
    }
}