

using System.Text.Json;

using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Api.Tests.Activities.Conversation;

public class EndOfConversationActivityTests
{
    private EndOfConversationActivity SetupEndOfConversationActivity()
    {
        return new EndOfConversationActivity()
        {
            Code = new EndOfConversationCode("completedSuccessfully"),
            Text = "The conversation has ended successfully.",
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
                Properties = new Dictionary<string, object>()
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
    public void EndOfConversationActivity_Props()
    {
        var activity = SetupEndOfConversationActivity();


        Assert.NotNull(activity.ToEndOfConversation());

        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.EndOfConversationActivity' to type 'Microsoft.Teams.Api.Activities.ConversationUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToConversationUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void EndOfConversationActivity_JsonSerialize()
    {
        var activity = SetupEndOfConversationActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Conversation/EndOfConversationActivity.json"
        ), json);
    }

    [Fact]
    public void EndOfConversationActivity_JsonSerialize_Derived_From_Class()
    {
        Activity activity = SetupEndOfConversationActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Conversation/EndOfConversationActivity.json"
        ), json);
    }

    [Fact]
    public void EndOfConversationActivity_JsonSerialize_Derived_From_Interface()
    {
        IActivity activity = SetupEndOfConversationActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Conversation/EndOfConversationActivity.json"
        ), json);
    }

    [Fact]
    public void EndOfConversationActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Conversation/EndOfConversationActivity.json");
        var activity = JsonSerializer.Deserialize<EndOfConversationActivity>(json);
        var expected = SetupEndOfConversationActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToEndOfConversation());
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.EndOfConversationActivity' to type 'Microsoft.Teams.Api.Activities.ConversationUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToConversationUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }


    [Fact]
    public void EndOfConversationActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Conversation/EndOfConversationActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = SetupEndOfConversationActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToEndOfConversation());
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.EndOfConversationActivity' to type 'Microsoft.Teams.Api.Activities.InstallUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToInstallUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }
}