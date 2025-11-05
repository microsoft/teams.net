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
    public void MeetingStartActivity_JsonSerialize_Object()
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
    public void MeetingStartActivity_JsonSerialize_Derived_From_Class()
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
    public void MeetingStartActivity_JsonSerialize_Derived_From_Interface()
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
        Assert.Equal(typeof(MeetingStartActivity), activity.Name.ToType());
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
        Assert.Equal(typeof(MeetingStartActivity), activity.Name.ToType());
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

    [Fact]
    public void MeetingStartActivity_JsonDeserialize_TeamsPayload_PascalCase()
    {
        // This test verifies that we can deserialize the actual JSON payload sent by Teams
        // which uses PascalCase for value object properties
        var json = @"{
            ""name"": ""application/vnd.microsoft.meetingStart"",
            ""type"": ""event"",
            ""timestamp"": ""2025-10-31T10:00:00.0000000Z"",
            ""id"": ""1761910695514"",
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
                ""Title"": ""Test Meeting"",
                ""Id"": ""MCMxOTptZWV0aW5nX01UUm1NVFE1TkRZdE1UWXlZaTAwTm1ObExXSTRaVFF0TjJJMU1UWXpNMlJrWVRnM0B0aHJlYWQudjIjMA=="",
                ""JoinUrl"": ""https://teams.microsoft.com/l/meetup-join/19%3ameeting_MTRmMTQ5NDYtMTYyYi00NmNlLWI4ZTQtN2I1MTYzM2RkYTg3%40thread.v2/0?context=%7b%22Tid%22%3a%22167c22a9-1b2e-439c-ad74-cc77e9e118d8%22%2c%22Oid%22%3a%2286a23cfc-f78e-424a-8947-7ae0ce242da1%22%7d"",
                ""StartTime"": ""2025-10-31T10:00:00.0000000Z""
            },
            ""locale"": ""en-US""
        }";

        var activity = JsonSerializer.Deserialize<MeetingStartActivity>(json);
        
        Assert.NotNull(activity);
        Assert.NotNull(activity.Value);
        Assert.Equal("MCMxOTptZWV0aW5nX01UUm1NVFE1TkRZdE1UWXlZaTAwTm1ObExXSTRaVFF0TjJJMU1UWXpNMlJrWVRnM0B0aHJlYWQudjIjMA==", activity.Value.Id);
        Assert.Equal("Scheduled", activity.Value.MeetingType);
        Assert.Equal("Test Meeting", activity.Value.Title);
        Assert.Equal("https://teams.microsoft.com/l/meetup-join/19%3ameeting_MTRmMTQ5NDYtMTYyYi00NmNlLWI4ZTQtN2I1MTYzM2RkYTg3%40thread.v2/0?context=%7b%22Tid%22%3a%22167c22a9-1b2e-439c-ad74-cc77e9e118d8%22%2c%22Oid%22%3a%2286a23cfc-f78e-424a-8947-7ae0ce242da1%22%7d", activity.Value.JoinUrl);
        Assert.Equal(new DateTime(2025, 10, 31, 10, 0, 0, DateTimeKind.Utc), activity.Value.StartTime);
    }
}