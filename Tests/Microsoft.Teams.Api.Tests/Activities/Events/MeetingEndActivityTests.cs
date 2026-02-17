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

    [Fact]
    public void MeetingEndActivity_JsonDeserialize_TeamsPayload_PascalCase()
    {
        // This test verifies that we can deserialize the actual JSON payload sent by Teams
        // which uses PascalCase for value object properties (as reported in the issue)
        var json = @"{
            ""name"": ""application/vnd.microsoft.meetingEnd"",
            ""type"": ""event"",
            ""timestamp"": ""2025-10-31T11:38:15.5375726Z"",
            ""id"": ""1761910695513"",
            ""channelId"": ""msteams"",
            ""serviceUrl"": ""https://smba.trafficmanager.net/emea/167c22a9-1b2e-439c-ad74-cc77e9e118d8/"",
            ""from"": {
                ""id"": ""29:1geTNfcvfJus0De5z4gr7HeHGMOuln9LY8aHFGtwBqhOl7ZYQFcM2CL1ODjhgHE1XTq3vBeeRlGGGPvFWi0BzRw"",
                ""name"": """",
                ""aadObjectId"": ""86a23cfc-f78e-424a-8947-7ae0ce242da1""
            },
            ""conversation"": {
                ""isGroup"": true,
                ""conversationType"": ""groupChat"",
                ""tenantId"": ""167c22a9-1b2e-439c-ad74-cc77e9e118d8"",
                ""id"": ""19:meeting_MTRmMTQ5NDYtMTYyYi00NmNlLWI4ZTQtN2I1MTYzM2RkYTg3@thread.v2""
            },
            ""recipient"": {
                ""id"": ""28:c9a052ed-f68c-4227-b081-01da0669c49c"",
                ""name"": ""teams-bot""
            },
            ""value"": {
                ""MeetingType"": ""Scheduled"",
                ""Title"": ""asdasd"",
                ""Id"": ""MCMxOTptZWV0aW5nX01UUm1NVFE1TkRZdE1UWXlZaTAwTm1ObExXSTRaVFF0TjJJMU1UWXpNMlJrWVRnM0B0aHJlYWQudjIjMA=="",
                ""JoinUrl"": ""https://teams.microsoft.com/l/meetup-join/19%3ameeting_MTRmMTQ5NDYtMTYyYi00NmNlLWI4ZTQtN2I1MTYzM2RkYTg3%40thread.v2/0?context=%7b%22Tid%22%3a%22167c22a9-1b2e-439c-ad74-cc77e9e118d8%22%2c%22Oid%22%3a%2286a23cfc-f78e-424a-8947-7ae0ce242da1%22%7d"",
                ""EndTime"": ""2025-10-31T11:38:15.5375726Z""
            },
            ""locale"": ""en-US""
        }";

        var activity = JsonSerializer.Deserialize<MeetingEndActivity>(json);

        Assert.NotNull(activity);
        Assert.NotNull(activity.Value);
        Assert.Equal("MCMxOTptZWV0aW5nX01UUm1NVFE1TkRZdE1UWXlZaTAwTm1ObExXSTRaVFF0TjJJMU1UWXpNMlJrWVRnM0B0aHJlYWQudjIjMA==", activity.Value.Id);
        Assert.Equal("Scheduled", activity.Value.MeetingType);
        Assert.Equal("asdasd", activity.Value.Title);
        Assert.Equal("https://teams.microsoft.com/l/meetup-join/19%3ameeting_MTRmMTQ5NDYtMTYyYi00NmNlLWI4ZTQtN2I1MTYzM2RkYTg3%40thread.v2/0?context=%7b%22Tid%22%3a%22167c22a9-1b2e-439c-ad74-cc77e9e118d8%22%2c%22Oid%22%3a%2286a23cfc-f78e-424a-8947-7ae0ce242da1%22%7d", activity.Value.JoinUrl);
        Assert.Equal(new DateTime(2025, 10, 31, 11, 38, 15, 537, DateTimeKind.Utc).AddTicks(5726), activity.Value.EndTime);
    }

    [Fact]
    public void MeetingEndActivity_JsonSerialize_PascalCase_RoundTrip()
    {
        // Verify that serialization produces PascalCase and can be deserialized back
        var activity = new MeetingEndActivity()
        {
            Value = new MeetingEndActivityValue()
            {
                Id = "testId123",
                MeetingType = "Scheduled",
                JoinUrl = "https://teams.microsoft.com/l/meetup-join/test",
                Title = "Test Meeting",
                EndTime = new DateTime(2025, 12, 10, 15, 0, 0, DateTimeKind.Utc),
            },
            Recipient = new Account()
            {
                Id = "recipientId",
                Name = "recipientName"
            },
            ChannelId = new ChannelId("msteams"),
        };

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        // Verify PascalCase in serialized JSON
        Assert.Contains("\"Id\":", json);
        Assert.Contains("\"MeetingType\":", json);
        Assert.Contains("\"JoinUrl\":", json);
        Assert.Contains("\"Title\":", json);
        Assert.Contains("\"EndTime\":", json);

        // Verify round-trip deserialization
        var deserialized = JsonSerializer.Deserialize<MeetingEndActivity>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(activity.Value.Id, deserialized.Value.Id);
        Assert.Equal(activity.Value.MeetingType, deserialized.Value.MeetingType);
        Assert.Equal(activity.Value.Title, deserialized.Value.Title);
        Assert.Equal(activity.Value.JoinUrl, deserialized.Value.JoinUrl);
        Assert.Equal(activity.Value.EndTime, deserialized.Value.EndTime);
    }

    [Fact]
    public void MeetingEndActivity_JsonDeserialize_TeamsPayload_As_EventActivity()
    {
        // Verify deserialization works when deserializing as EventActivity base class
        var json = @"{
            ""name"": ""application/vnd.microsoft.meetingEnd"",
            ""type"": ""event"",
            ""channelId"": ""msteams"",
            ""value"": {
                ""MeetingType"": ""Scheduled"",
                ""Title"": ""Test Meeting"",
                ""Id"": ""testId"",
                ""JoinUrl"": ""https://teams.microsoft.com/test"",
                ""EndTime"": ""2025-12-10T15:00:00Z""
            }
        }";

        var activity = JsonSerializer.Deserialize<EventActivity>(json);

        Assert.NotNull(activity);
        Assert.True(activity.Name.IsMeetingEnd);
        var meetingEndActivity = activity as MeetingEndActivity;
        Assert.NotNull(meetingEndActivity);
        Assert.Equal("testId", meetingEndActivity.Value.Id);
        Assert.Equal("Scheduled", meetingEndActivity.Value.MeetingType);
    }

    [Fact]
    public void MeetingEndActivity_JsonDeserialize_TeamsPayload_As_IActivity()
    {
        // Verify deserialization works when deserializing as IActivity interface
        var json = @"{
            ""name"": ""application/vnd.microsoft.meetingEnd"",
            ""type"": ""event"",
            ""channelId"": ""msteams"",
            ""value"": {
                ""MeetingType"": ""Adhoc"",
                ""Title"": ""Quick Meeting"",
                ""Id"": ""meetingId456"",
                ""JoinUrl"": ""https://teams.microsoft.com/join/456"",
                ""EndTime"": ""2025-12-10T16:30:00Z""
            }
        }";

        var activity = JsonSerializer.Deserialize<IActivity>(json);

        Assert.NotNull(activity);
        var meetingEndActivity = activity as MeetingEndActivity;
        Assert.NotNull(meetingEndActivity);
        Assert.Equal("meetingId456", meetingEndActivity.Value.Id);
        Assert.Equal("Adhoc", meetingEndActivity.Value.MeetingType);
        Assert.Equal("Quick Meeting", meetingEndActivity.Value.Title);
    }
}