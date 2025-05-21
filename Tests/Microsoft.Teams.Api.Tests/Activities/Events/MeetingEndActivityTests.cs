using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;

namespace Microsoft.Teams.Api.Tests.Activities.Events;

public class MeetingEndActivityTests
{
    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        IndentSize = 2,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public MeetingEndActivity SetupMeetingEndActivity()
    {
        return new MeetingEndActivity()
        {
            Value = new MeetingEndActivityValue()
            {
                Id = "id",
                MeetingType = "meetingType",
                JoinUrl = "https://teams.meetingjoin.url/somevalues",
                Title = "Meeting For Teams.net",
                EndTime = new DateTime(2025, 1, 1, 5, 30, 00),
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
    public void MeetingEndActivity_Props()
    {
        var activity = SetupMeetingEndActivity();

        Assert.NotNull(activity.ToMeetingEnd());
        ActivityType expectedEventType = new ActivityType("event");
        Assert.Equal(expectedEventType.ToString(), activity.Type.Value);
        Assert.True(activity.Name.IsMeetingEnd);
        Assert.False(activity.Name.IsMeetingStart);
        Assert.False(activity.IsStreaming);
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.Events.MeetingEndActivity' to type 'Microsoft.Teams.Api.Activities.MessageActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToMessage());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void MeetingEndActivity_JsonSerialize()
    {
        var activity = SetupMeetingEndActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/MeetingEndActivity.json"
        ), json);
    }


    [Fact]
    public void MeetingEndActivity_JsonSerialize_Object()
    {
        MeetingEndActivity activity = SetupMeetingEndActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Event.Application/vnd.microsoft.meetingEnd";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/MeetingEndActivity.json"
        ), json);
    }

    [Fact]
    public void MeetingEndActivity_JsonSerialize_Derived_From_Class()
    {
        EventActivity activity = SetupMeetingEndActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Event.Application/vnd.microsoft.meetingEnd";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.False(activity.Name.IsMeetingStart);
        Assert.True(activity.Name.IsMeetingEnd);
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/MeetingEndActivity.json"
        ), json);
    }

    [Fact]
    public void MeetingEndActivity_JsonSerialize_Derived_From_Interface()
    {
        IActivity activity = SetupMeetingEndActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Event.Application/vnd.microsoft.meetingEnd";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/MeetingEndActivity.json"
        ), json);
    }

    [Fact]
    public void MeetingEndActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Events/MeetingEndActivity.json");
        var activity = JsonSerializer.Deserialize<MeetingEndActivity>(json);
        var expected = SetupMeetingEndActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.Equal(typeof(MeetingEndActivity), activity.Name.ToType());
        Assert.Equal("Application/vnd.microsoft.meetingEnd", activity.Name.ToPrettyString());
        Assert.NotNull(activity.ToMeetingEnd());
    }


    [Fact]
    public void MeetingEndActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Events/MeetingEndActivity.json");
        var activity = JsonSerializer.Deserialize<EventActivity>(json);
        var expected = SetupMeetingEndActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToEvent());
        Assert.Equal(typeof(MeetingEndActivity), activity.Name.ToType());
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.Events.MeetingEndActivity' to type 'Microsoft.Teams.Api.Activities.InstallUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToInstallUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void MeetingEndActivity_JsonDeserialize_Activity_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Events/MeetingEndActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = SetupMeetingEndActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToEvent());
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.Events.MeetingEndActivity' to type 'Microsoft.Teams.Api.Activities.InstallUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToInstallUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }
}