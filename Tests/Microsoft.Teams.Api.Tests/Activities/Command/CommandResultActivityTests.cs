

using System.Text.Json;

using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Api.Tests.Activities.Command;

public class CommandResultActivityTests
{
    private CommandResultActivity SetupCommandResultActivity()
    {
        return new CommandResultActivity()
        {
            Name = "TestCommand",
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
    public void CommandResultActivity_Props()
    {
        var activity = SetupCommandResultActivity();


        Assert.NotNull(activity.ToCommandResult());

        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.CommandResultActivity' to type 'Microsoft.Teams.Api.Activities.ConversationUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToConversationUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void CommandResultActivity_JsonSerialize()
    {
        var activity = SetupCommandResultActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Command/CommandResultActivity.json"
        ), json);
    }

    [Fact]
    public void CommandResultActivity_JsonSerialize_Derived_From_Class()
    {
        Activity activity = SetupCommandResultActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Command/CommandResultActivity.json"
        ), json);
    }

    [Fact]
    public void CommandResultActivity_JsonSerialize_Derived_From_Interface()
    {
        IActivity activity = SetupCommandResultActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Command/CommandResultActivity.json"
        ), json);
    }

    [Fact]
    public void CommandResultActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Command/CommandResultActivity.json");
        var activity = JsonSerializer.Deserialize<CommandResultActivity>(json);
        var expected = SetupCommandResultActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToCommandResult());
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.CommandResultActivity' to type 'Microsoft.Teams.Api.Activities.ConversationUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToConversationUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }


    [Fact]
    public void CommandResultActivity_JsonDeserialize_Derived_From_Class()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Command/CommandResultActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = SetupCommandResultActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToCommandResult());
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.CommandResultActivity' to type 'Microsoft.Teams.Api.Activities.InstallUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToInstallUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

}