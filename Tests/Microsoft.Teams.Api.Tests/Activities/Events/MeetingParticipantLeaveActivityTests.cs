using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Api.Entities;

using static Microsoft.Teams.Api.Activities.Events.MeetingParticipantLeaveActivityValue;

namespace Microsoft.Teams.Api.Tests.Activities.Events;

public class MeetingParticipantLeaveActivityTests
{
    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        IndentSize = 2,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public MeetingParticipantLeaveActivity SetupMeetingParticipantLeaveActivity()
    {
        return new MeetingParticipantLeaveActivity()
        {
            Value = new MeetingParticipantLeaveActivityValue()
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
    public void MeetingParticipantLeaveActivity_Props()
    {
        var activity = SetupMeetingParticipantLeaveActivity();

        Assert.NotNull(activity.ToMeetingParticipantLeave());
        ActivityType expectedEventType = new ActivityType("event");
        Assert.Equal(expectedEventType.ToString(), activity.Type.Value);
        Assert.True(activity.Name.IsMeetingParticipantLeave);
        Assert.False(activity.Name.IsMeetingParticipantJoin);
        Assert.True(activity.IsStreaming);
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.Events.MeetingParticipantLeaveActivity' to type 'Microsoft.Teams.Api.Activities.MessageActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToMessage());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void MeetingParticipantLeaveActivity_JsonSerialize()
    {
        var activity = SetupMeetingParticipantLeaveActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/MeetingParticipantLeaveActivity.json"
        ), json);
    }


    [Fact]
    public void MeetingParticipantLeaveActivity_JsonSerialize_Derived()
    {
        MeetingParticipantLeaveActivity activity = SetupMeetingParticipantLeaveActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Event.Application/vnd.microsoft.meetingParticipantLeave";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/MeetingParticipantLeaveActivity.json"
        ), json);
    }

    [Fact]
    public void MeetingParticipantLeaveActivity_JsonSerialize_Derived_Type()
    {
        EventActivity activity = SetupMeetingParticipantLeaveActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Event.Application/vnd.microsoft.meetingParticipantLeave";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.True(activity.Name.IsMeetingParticipantLeave);
        Assert.False(activity.Name.IsMeetingParticipantJoin);
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/MeetingParticipantLeaveActivity.json"
        ), json);
    }

    [Fact]
    public void MeetingParticipantLeaveActivity_JsonSerialize_Derived_Interface()
    {
        IActivity activity = SetupMeetingParticipantLeaveActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Event.Application/vnd.microsoft.meetingParticipantLeave";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/MeetingParticipantLeaveActivity.json"
        ), json);
    }

    [Fact]
    public void MeetingParticipantLeaveActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Events/MeetingParticipantLeaveActivity.json");
        var activity = JsonSerializer.Deserialize<MeetingParticipantLeaveActivity>(json);
        var expected = SetupMeetingParticipantLeaveActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.Equal("Application/vnd.microsoft.meetingParticipantLeave", activity.Name.ToPrettyString());
        Assert.Equal(typeof(MeetingParticipantLeaveActivity), activity.Name.ToType());
        Assert.NotNull(activity.ToMeetingParticipantLeave());
    }


    [Fact]
    public void MeetingParticipantLeaveActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Events/MeetingParticipantLeaveActivity.json");
        var activity = JsonSerializer.Deserialize<EventActivity>(json);
        var expected = SetupMeetingParticipantLeaveActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToEvent());
        Assert.Equal(typeof(MeetingParticipantLeaveActivity), activity.Name.ToType());
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.Events.MeetingParticipantLeaveActivity' to type 'Microsoft.Teams.Api.Activities.InstallUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToInstallUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void MeetingParticipantLeaveActivity_JsonDeserialize_Activity_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Events/MeetingParticipantLeaveActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = SetupMeetingParticipantLeaveActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToEvent());
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.Events.MeetingParticipantLeaveActivity' to type 'Microsoft.Teams.Api.Activities.EndOfConversationActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToEndOfConversation());
        Assert.Equal(expectedSubmitException, ex.Message);
    }
}