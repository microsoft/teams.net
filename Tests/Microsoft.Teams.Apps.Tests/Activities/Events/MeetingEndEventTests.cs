
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities.Events;
using Microsoft.Teams.Apps.Testing.Plugins;

using static Microsoft.Teams.Apps.Activities.Events.Event;

namespace Microsoft.Teams.Apps.Tests.Activities.Events;

public class MeetingEndEventTests
{
    private readonly App _app = new();
    private readonly TestPlugin _plugin = new();
    private readonly MeetingActivityController _controller = new();
    private readonly IToken _token = Globals.Token;

    public MeetingEndEventTests()
    {
        _app.AddPlugin(_plugin);
        _app.AddController(_controller);
        _token = Globals.Token;
    }

    [Fact]
    public async Task Should_CallHandler_OnMeetingEndEvent()
    {
        // Arrange
        var handlerCalled = false;
        var eventContext = default(IContext<MeetingEndActivity>);

        _app.OnMeetingEnd(context =>
        {
            handlerCalled = true;
            eventContext = context;
            return Task.FromResult<object?>(null);
        });

        // Create a MeetingEndActivity
        var meetingEndActivity = new MeetingEndActivity
        {
            Value = new MeetingEndActivityValue
            {
                Id = "mock-meeting-id",
                MeetingType = "scheduled",
                JoinUrl = "https://teams.microsoft.com/meeting/join/123",
                Title = "Test Meeting",
                EndTime = DateTime.UtcNow
            }
        };

        // Act
        var res = await _plugin.Do(_token, meetingEndActivity);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.True(handlerCalled, "The MeetingEnd event handler should be called");
        Assert.NotNull(eventContext);
        Assert.IsType<MeetingEndActivity>(eventContext.Activity);
        Assert.Equal("mock-meeting-id", eventContext.Activity.Value.Id);
    }

    [Fact]
    public async Task Should_NotCallHandler_ForOtherEventTypes()
    {
        // Arrange
        var handlerCalled = false;

        _app.OnMeetingEnd(context =>
        {
            handlerCalled = true;
            return Task.FromResult<object?>(null);
        });

        // Act - Send a different activity type
        var res = await _plugin.Do(_token, new MessageActivity("hello world"));

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.False(handlerCalled, "The MeetingEnd event handler should not be called for other activity types");
    }

    [Fact]
    public void MeetingEndAttribute_Select_ReturnsTrueForMeetingEndActivity()
    {
        // Arrange
        var attribute = new MeetingEndAttribute();
        var activity = new MeetingEndActivity
        {
            Value = new MeetingEndActivityValue
            {
                Id = "mock-meeting-id",
                MeetingType = "scheduled",
                JoinUrl = "https://teams.microsoft.com/meeting/join/123",
                Title = "Test Meeting",
                EndTime = DateTime.UtcNow
            }
        };

        // Act
        var result = attribute.Select(activity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MeetingEndAttribute_Select_ReturnsFalseForOtherActivities()
    {
        // Arrange
        var attribute = new MeetingEndAttribute();
        var activity = new MessageActivity("hello world");

        // Act
        var result = attribute.Select(activity);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task MeetingEndAttribute_Controller_Call()
    {
        var activity = new MeetingEndActivity
        {
            Value = new MeetingEndActivityValue
            {
                Id = "mock-meeting-id",
                MeetingType = "scheduled",
                JoinUrl = "https://teams.microsoft.com/meeting/join/123",
                Title = "Test Meeting",
                EndTime = DateTime.UtcNow
            }
        };

        var res = await _app.Process<TestPlugin>(_token, activity);

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal("meetingEndMethod", _controller.MethodCalled);
    }
}