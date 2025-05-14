using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;

namespace Microsoft.Teams.Api.Tests.Activities.Events;

public class MeetingStartActivityTests
{
    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        IndentSize = 2,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public MeetingStartActivity SetupMeetingStartActivity()
    {
        return new MeetingStartActivity()
        {
            Value = new MeetingStartActivityValue()
            {
                Id = "id",
                MeetingType = "meetingType",
                JoinUrl = "https://teams.meetingjoin.url/somevalues",
                Title = "Meeting For Teams.net",
                StartTime = new DateTime(2025, 1, 1, 4, 30, 00),
            },
            Recipient = new Account()
            {
                Id = "recipientId",
                Name = "recipientName"
            },
            ChannelId = new ChannelId("msteams"),
        };
    }

    [Fact]
    public void MeetingStartActivity_Props()
    {
        var activity = SetupMeetingStartActivity();

        Assert.NotNull(activity.ToMeetingStart());
        ActivityType expectedEventType = new ActivityType("event");
        Assert.Equal(expectedEventType.ToString(), activity.Type.Value);
        Assert.True(activity.Name.IsMeetingStart);
        Assert.False(activity.IsStreaming);
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.Events.MeetingStartActivity' to type 'Microsoft.Teams.Api.Activities.MessageActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToMessage());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void MeetingStartActivity_JsonSerialize()
    {
        var activity = SetupMeetingStartActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/MeetingStartActivity.json"
        ), json);
    }


    [Fact]
    public void MeetingStartActivity_JsonSerialize_Derived()
    {
        MeetingStartActivity activity = SetupMeetingStartActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Event.Application/vnd.microsoft.meetingStart";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/MeetingStartActivity.json"
        ), json);
    }

    [Fact]
    public void MeetingStartActivity_JsonSerialize_Derived_Type()
    {
        EventActivity activity = SetupMeetingStartActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Event.Application/vnd.microsoft.meetingStart";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.False(activity.Name.IsReadReceipt);
        Assert.True(activity.Name.IsMeetingStart);
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/MeetingStartActivity.json"
        ), json);
    }

    [Fact]
    public void MeetingStartActivity_JsonSerialize_Derived_Interface()
    {
        IActivity activity = SetupMeetingStartActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Event.Application/vnd.microsoft.meetingStart";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/MeetingStartActivity.json"
        ), json);
    }

    [Fact]
    public void MeetingStartActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Events/MeetingStartActivity.json");
        var activity = JsonSerializer.Deserialize<MeetingStartActivity>(json);
        var expected = SetupMeetingStartActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.Equal("Application/vnd.microsoft.meetingStart", activity.Name.ToPrettyString());
        Assert.NotNull(activity.ToMeetingStart());
    }


    [Fact]
    public void MeetingStartActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Events/MeetingStartActivity.json");
        var activity = JsonSerializer.Deserialize<EventActivity>(json);
        var expected = SetupMeetingStartActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToEvent());
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.Events.MeetingStartActivity' to type 'Microsoft.Teams.Api.Activities.InstallUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToInstallUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void MeetingStartActivity_JsonDeserialize_Activity_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Events/MeetingStartActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = SetupMeetingStartActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToEvent());
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.Events.MeetingStartActivity' to type 'Microsoft.Teams.Api.Activities.InstallUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToInstallUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }
}