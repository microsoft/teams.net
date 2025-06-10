using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Activities.Events;
using Microsoft.Teams.Apps.Testing.Plugins;

using static Microsoft.Teams.Apps.Activities.Events.Event;


namespace Microsoft.Teams.Apps.Tests.Activities.Events;

public class MeetingStartEventTests
{
    private readonly App _app = new();
    private readonly TestPlugin _plugin = new();
    private readonly MeetingActivityController _controller = new();
    private readonly IToken _token = Globals.Token;

    public MeetingStartEventTests()
    {
        _app.AddPlugin(_plugin);
        _app.AddController(_controller);
        _token = Globals.Token;
    }

    [Fact]
    public async Task Should_CallHandler_OnMeetingStartEvent()
    {
        // Arrange
        var handlerCalled = false;
        var eventContext = default(IContext<MeetingStartActivity>);

        _app.OnMeetingStart(context =>
        {
            handlerCalled = true;
            eventContext = context;
            return Task.FromResult<object?>(null);
        });

        // Create a MeetingStartActivity
        var meetingStartActivity = new MeetingStartActivity
        {
            Value = new MeetingStartActivityValue
            {
                Id = "mock-meeting-id",
                MeetingType = "scheduled",
                JoinUrl = "https://teams.microsoft.com/meeting/join/123",
                Title = "Test Meeting",
                StartTime = DateTime.UtcNow
            }
        };

        // Act
        var res = await _plugin.Do(_token, meetingStartActivity);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.True(handlerCalled, "The MeetingStart event handler should be called");
        Assert.NotNull(eventContext);
        Assert.IsType<MeetingStartActivity>(eventContext.Activity);
        Assert.Equal("mock-meeting-id", eventContext.Activity.Value.Id);
    }

    [Fact]
    public async Task Should_NotCallHandler_ForOtherEventTypes()
    {
        // Arrange
        var handlerCalled = false;

        _app.OnMeetingStart(context =>
        {
            handlerCalled = true;
            return Task.FromResult<object?>(null);
        });

        // Act - Send a different activity type
        var res = await _plugin.Do(_token, new MessageActivity("hello world"));

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.False(handlerCalled, "The MeetingStart event handler should not be called for other activity types");
    }

    [Fact]
    public void MeetingStartAttribute_Select_ReturnsTrueForMeetingStartActivity()
    {
        // Arrange
        var attribute = new MeetingStartAttribute();
        var activity = new MeetingStartActivity
        {
            Value = new MeetingStartActivityValue
            {
                Id = "mock-meeting-id",
                MeetingType = "scheduled",
                JoinUrl = "https://teams.microsoft.com/meeting/join/123",
                Title = "Test Meeting",
                StartTime = DateTime.UtcNow
            }
        };

        // Act
        var result = attribute.Select(activity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MeetingStartAttribute_Select_ReturnsFalseForOtherActivities()
    {
        // Arrange
        var attribute = new MeetingStartAttribute();
        var activity = new MessageActivity("hello world");

        // Act
        var result = attribute.Select(activity);

        // Assert
        Assert.False(result);
    }


    [Fact]
    public async Task MeetingStartAttribute_Controller_Call()
    {
        var activity = new MeetingStartActivity
        {
            Value = new MeetingStartActivityValue
            {
                Id = "mock-meeting-id",
                MeetingType = "scheduled",
                JoinUrl = "https://teams.microsoft.com/meeting/join/123",
                Title = "Test Meeting",
                StartTime = DateTime.UtcNow
            }
        };

        var res = await _app.Process<TestPlugin>(_token, activity);

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal("meetingStartMethod", _controller.MethodCalled);
    }

}