using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Api.Entities;

namespace Microsoft.Teams.Api.Tests.Activities.Events;

public class ReadReceiptActivityTests
{
    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        IndentSize = 2,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ReadReceiptActivity SetupReadReceiptActivity()
    {
        return new ReadReceiptActivity()
        {
            Id = "readReceiptId",
            ChannelData = new ChannelData()
            {
                EventType = "readReceipt",
                Notification = new Notification(),
                Channel = new Channel()
                {
                    Id = "channelId",
                    Name = "channelName",
                    Type = new ChannelType("standard"),
                },
                Settings = new ChannelDataSettings()
                {
                    SelectedChannel = new Channel()
                    {
                        Id = "selectedChannelId",
                        Name = "selectedChannelName",
                        Type = new ChannelType("standard"),
                    },
                    Properties = new Dictionary<string, object?>()
                    {
                        { "channelDataSettingskey1", "value1" },
                        { "channelDataSettingskey2", "value2" },
                    }
                },
                Team = new Team()
                {
                    Id = "teamId",
                    Name = "teamName",
                    Type = new TeamType("standard"),
                },
                Tenant = new Tenant()
                {
                    Id = "tenantId",
                },
                App = new App()
                {
                    Id = "appId",
                    Properties = new Dictionary<string, object?>()
                    {
                        { "appPropKey1", "value1" },
                        { "appPropKey2", "value2" },
                    }
                },
                FeedbackLoopEnabled = true,
                Properties = new Dictionary<string, object?>()
                {
                    { "key1", "value1" },
                    { "key2", "value2" },
                },
                StreamId = "streamId",
                StreamSequence = 3,
                StreamType = new StreamType("streaming"),
            },
            Conversation = new Api.Conversation()
            {
                Id = "conversationId",
                Name = "conversationName",
                Type = new ConversationType("group"),
            },
            ChannelId = new ChannelId("webchat"),
            Recipient = new Account()
            {
                Id = "recipientId",
                Name = "recipientName",
            },
            ReplyToId = "replyToId",
        };
    }

    [Fact]
    public void ReadReceiptActivity_Props()
    {
        var activity = SetupReadReceiptActivity();

        Assert.NotNull(activity.ToReadReceipt());
        ActivityType expectedEventType = new ActivityType("event");
        Assert.Equal(expectedEventType.ToString(), activity.Type.Value);
        Assert.True(activity.Name.IsReadReceipt);
        Assert.False(activity.IsStreaming);
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.Events.ReadReceiptActivity' to type 'Microsoft.Teams.Api.Activities.MessageActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToMessage());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void ReadReceiptActivity_JsonSerialize()
    {
        var activity = SetupReadReceiptActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/ReadReceiptActivity.json"
        ), json);
    }


    [Fact]
    public void ReadReceiptActivity_JsonSerialize_Object()
    {
        ReadReceiptActivity activity = SetupReadReceiptActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Event.Application/vnd.microsoft.readReceipt";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/ReadReceiptActivity.json"
        ), json);
    }

    [Fact]
    public void ReadReceiptActivity_JsonSerialize_Derived_From_Class()
    {
        EventActivity activity = SetupReadReceiptActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Event.Application/vnd.microsoft.readReceipt";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(typeof(ReadReceiptActivity), activity.Name.ToType());
        Assert.True(activity.Name.IsReadReceipt);
        Assert.False(activity.Name.IsMeetingStart);
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/ReadReceiptActivity.json"
        ), json);
    }

    [Fact]
    public void ReadReceiptActivity_JsonSerialize_Derived_From_Interface()
    {
        IActivity activity = SetupReadReceiptActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Event.Application/vnd.microsoft.readReceipt";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Events/ReadReceiptActivity.json"
        ), json);
    }

    [Fact]
    public void ReadReceiptActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Events/ReadReceiptActivity.json");
        var activity = JsonSerializer.Deserialize<ReadReceiptActivity>(json);
        var expected = SetupReadReceiptActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.Equal(typeof(ReadReceiptActivity), activity.Name.ToType());
        Assert.Equal("Application/vnd.microsoft.readReceipt", activity.Name.ToPrettyString());
        Assert.NotNull(activity.ToReadReceipt());
    }


    [Fact]
    public void ReadReceiptActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Events/ReadReceiptActivity.json");
        var activity = JsonSerializer.Deserialize<EventActivity>(json);
        var expected = SetupReadReceiptActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToEvent());
        Assert.Equal(typeof(ReadReceiptActivity), activity.Name.ToType());
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.Events.ReadReceiptActivity' to type 'Microsoft.Teams.Api.Activities.InstallUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToInstallUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void ReadReceiptActivity_JsonDeserialize_Activity_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Events/ReadReceiptActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = SetupReadReceiptActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToEvent());
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.Events.ReadReceiptActivity' to type 'Microsoft.Teams.Api.Activities.InstallUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToInstallUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }
}