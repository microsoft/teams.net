using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Api.Entities;

using static Microsoft.Teams.Api.Activities.Events.MeetingParticipantJoinActivityValue;

namespace Microsoft.Teams.Api.Tests.Activities.Events;

public class MeetingParticipantJoinActivityTests
{
    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        IndentSize = 2,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public MeetingParticipantJoinActivity SetupMeetingParticipantJoinActivity()
    {
        return new MeetingParticipantJoinActivity()
        {
            Value = new MeetingParticipantJoinActivityValue()
            {
                Members = new List<Member>() {
                    new Member()
                    {
                        User = new Account()
                        {
                            Id = "userId",
                            Name = "userName"
                        },
                        Meeting = new Meeting()
                        {
                            InMeeting = true,
                            Role = Role.User,
                        }
                    },
                    new Member()
                    {
                        User = new Account()
                        {
                            Id = "botId",
                            Name = "BotUser"
                        },
                        Meeting = new Meeting()
                        {
                            InMeeting = true,
                            Role = Role.Bot,
                        }
                    }
                },

            },
            Recipient = new Account()
            {
                Id = "recipientId",
                Name = "recipientName"
            },
            ChannelId = new ChannelId("msteams"),
            Entities = new List<IEntity>()
            {
                new StreamInfoEntity()
               {
                   StreamId = "strId",
                   StreamSequence = 3,
                   StreamType = new StreamType("streaming")
               }
            },

        };
    }

    [Fact]
    public void MeetingParticipantJoinActivity_Props()
    {
        var activity = SetupMeetingParticipantJoinActivity();

        Assert.NotNull(activity.ToMeetingParticipantJoin());
        ActivityType expectedEventType = new ActivityType("event");
        Assert.Equal(expectedEventType.ToString(), activity.Type.Value);
        Assert.True(activity.Name.IsMeetingParticipantJoin);
        Assert.False(activity.Name.IsMeetingStart);
        Assert.True(activity.IsStreaming);
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.Events.MeetingParticipantJoinActivity' to type 'Microsoft.Teams.Api.Activities.MessageActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToMessage());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void MeetingParticipantJoinActivity_JsonSerialize()
    {
        var activity = SetupMeetingParticipantJoinActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/MeetingParticipantJoinActivity.json"
        ), json);
    }


    [Fact]
    public void MeetingParticipantJoinActivity_JsonSerialize_Derived()
    {
        MeetingParticipantJoinActivity activity = SetupMeetingParticipantJoinActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Event.Application/vnd.microsoft.meetingParticipantJoin";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/MeetingParticipantJoinActivity.json"
        ), json);
    }

    [Fact]
    public void MeetingParticipantJoinActivity_JsonSerialize_Derived_Type()
    {
        EventActivity activity = SetupMeetingParticipantJoinActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Event.Application/vnd.microsoft.meetingParticipantJoin";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.True(activity.Name.IsMeetingParticipantJoin);
        Assert.False(activity.Name.IsMeetingEnd);
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/MeetingParticipantJoinActivity.json"
        ), json);
    }

    [Fact]
    public void MeetingParticipantJoinActivity_JsonSerialize_Derived_Interface()
    {
        IActivity activity = SetupMeetingParticipantJoinActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Event.Application/vnd.microsoft.meetingParticipantJoin";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/MeetingParticipantJoinActivity.json"
        ), json);
    }

    [Fact]
    public void MeetingParticipantJoinActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Events/MeetingParticipantJoinActivity.json");
        var activity = JsonSerializer.Deserialize<MeetingParticipantJoinActivity>(json);
        var expected = SetupMeetingParticipantJoinActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.Equal("Application/vnd.microsoft.meetingParticipantJoin", activity.Name.ToPrettyString());
        Assert.NotNull(activity.ToMeetingParticipantJoin());
    }


    [Fact]
    public void MeetingParticipantJoinActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Events/MeetingParticipantJoinActivity.json");
        var activity = JsonSerializer.Deserialize<EventActivity>(json);
        var expected = SetupMeetingParticipantJoinActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToEvent());
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.Events.MeetingParticipantJoinActivity' to type 'Microsoft.Teams.Api.Activities.InstallUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToInstallUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void MeetingParticipantJoinActivity_JsonDeserialize_Activity_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Events/MeetingParticipantJoinActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = SetupMeetingParticipantJoinActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToEvent());
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.Events.MeetingParticipantJoinActivity' to type 'Microsoft.Teams.Api.Activities.InstallUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToInstallUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }
}