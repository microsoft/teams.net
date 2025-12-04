

using System.Text.Json;

using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Api.Tests.Activities.Conversation;

public class ConversationUpdateActivityTests
{
    private ConversationUpdateActivity SetupConversationUpdateActivity()
    {
        return new ConversationUpdateActivity()
        {
            TopicName = "topicName",
            MembersAdded =
            [
                new Account { Id = "userId1", Name = "User One" },
                new Account { Id = "userId2", Name = "User Two" }
            ],
            MembersRemoved =
            [
                new Account { Id = "userId3", Name = "User Three" },

            ],
            ChannelId = new ChannelId("msteams"),
            Conversation = new Api.Conversation()
            {
                Type = new ConversationType("channel"),
                Id = "someguid",
                TenantId = "tenantId",
                Name = "channelName",
                IsGroup = false,

            },
            From = new Account()
            {
                Id = "botId",
                Name = "Bot user",
                Role = new Role("bot"),
                AadObjectId = "aadObjectId",
                Properties = new Bot.Core.Schema.ExtendedPropertiesDictionary()
                {
                    { "key1", "value1" },
                    { "key2", "value2" },
                },
            },
            Recipient = new Account()
            {
                Id = "userId1",
                Name = "User One"
            },
        };
    }
    [Fact]
    public void ConversationUpdateActivity_Props()
    {
        var activity = SetupConversationUpdateActivity();

        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.ConversationUpdateActivity' to type 'Microsoft.Teams.Api.Activities.EndOfConversationActivity'.";

        Assert.NotNull(activity.ToConversationUpdate());

        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToEndOfConversation());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void ConversationUpdateActivity_JsonSerialize()
    {
        var activity = SetupConversationUpdateActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Conversation/ConversationUpdateActivity.json"
        ), json);
    }

    [Fact]
    public void ConversationUpdateActivity_JsonSerialize_Derived_From_Class()
    {
        Activity activity = SetupConversationUpdateActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Conversation/ConversationUpdateActivity.json"
        ), json);
    }

    [Fact]
    public void ConversationUpdateActivity_JsonSerialize_Derived_From_Interface()
    {
        IActivity activity = SetupConversationUpdateActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Conversation/ConversationUpdateActivity.json"
        ), json);
    }


    [Fact]
    public void ConversationUpdateActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Conversation/ConversationUpdateActivity.json");
        var activity = JsonSerializer.Deserialize<ConversationUpdateActivity>(json);
        var expected = SetupConversationUpdateActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToConversationUpdate());
    }


    [Fact]
    public void ConversationUpdateActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Conversation/ConversationUpdateActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = SetupConversationUpdateActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToConversationUpdate());
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.ConversationUpdateActivity' to type 'Microsoft.Teams.Api.Activities.InstallUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToInstallUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }
}