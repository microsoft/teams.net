
using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities.Events;
using Microsoft.Teams.Apps.Testing.Plugins;
using Microsoft.Extensions.Logging;
using Moq;
using static Microsoft.Teams.Apps.Activities.Events.Event;

namespace Microsoft.Teams.Apps.Tests.Activities.Events;

public class MeetingJoinEventTests
{
    private readonly Mock<ILogger<App>> _logger = new();
    private readonly App _app;
    private readonly TestPlugin _plugin = new();
    private readonly MeetingActivityController _controller = new();
    private readonly IToken _token = Globals.Token;

    public MeetingJoinEventTests()
    {
        _app = new App(_logger.Object);
        _app.AddPlugin(_plugin);
        _app.AddController(_controller);
        _token = Globals.Token;
    }

    [Fact]
    public async Task Should_CallHandler_OnMeetingJoinEvent()
    {
        // Arrange
        var handlerCalled = false;
        var eventContext = default(IContext<MeetingParticipantJoinActivity>);

        _app.OnMeetingJoin(context =>
        {
            handlerCalled = true;
            eventContext = context;
            return Task.FromResult<object?>(null);
        });

        // Create a MeetingJoinActivity
        var meetingJoinActivity = new MeetingParticipantJoinActivity
        {
            Value = new MeetingParticipantJoinActivityValue
            {
                Members = new List<MeetingParticipantJoinActivityValue.Member>
                {
                    new MeetingParticipantJoinActivityValue.Member
                    {
                        User = new Account { Id = "user1", Name = "Test User" },
                        Meeting = new MeetingParticipantJoinActivityValue.Meeting
                        {
                            InMeeting = true,
                            Role = Role.User
                        }
                    },

                }
            }
        };

        // Act
        var res = await _plugin.Do(_token, meetingJoinActivity);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.True(handlerCalled, "The MeetingJoin event handler should be called");
        Assert.NotNull(eventContext);
        Assert.IsType<MeetingParticipantJoinActivity>(eventContext.Activity);
        Assert.Single(eventContext.Activity.Value.Members);
    }

    [Fact]
    public async Task Should_NotCallHandler_ForOtherEventTypes()
    {
        // Arrange
        var handlerCalled = false;

        _app.OnMeetingJoin(context =>
        {
            handlerCalled = true;
            return Task.FromResult<object?>(null);
        });

        // Act - SJoin a different activity type
        var res = await _plugin.Do(_token, new MessageActivity("hello world"));

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.False(handlerCalled, "The MeetingJoin event handler should not be called for other activity types");
    }

    [Fact]
    public void MeetingJoinAttribute_Select_ReturnsTrueForMeetingJoinActivity()
    {
        // Arrange
        var attribute = new MeetingJoinAttribute();
        var activity = new MeetingParticipantJoinActivity
        {
            Value = new MeetingParticipantJoinActivityValue
            {
                Members = new List<MeetingParticipantJoinActivityValue.Member>
                {
                    new MeetingParticipantJoinActivityValue.Member
                    {
                        User = new Account { Id = "user1", Name = "Test User" },
                        Meeting = new MeetingParticipantJoinActivityValue.Meeting
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
    public void MeetingJoinAttribute_Select_ReturnsFalseForOtherActivities()
    {
        // Arrange
        var attribute = new MeetingJoinAttribute();
        var activity = new MessageActivity("hello world");

        // Act
        var result = attribute.Select(activity);

        // Assert
        Assert.False(result);
    }


    [Fact]
    public async Task MeetingJoinAttribute_Controller_Call()
    {
        var activity = new MeetingParticipantJoinActivity
        {
            Value = new MeetingParticipantJoinActivityValue
            {
                Members = new List<MeetingParticipantJoinActivityValue.Member>
                {
                    new MeetingParticipantJoinActivityValue.Member
                    {
                        User = new Account { Id = "user1", Name = "Test User" },
                        Meeting = new MeetingParticipantJoinActivityValue.Meeting
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
        Assert.Equal("meetingJoinMethod", _controller.MethodCalled);
    }
}