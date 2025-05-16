

using System.Text.Json;

using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Api.Tests.Activities.Command;

public class CommandActivityTests
{
    private CommandActivity SetupCommandActivity()
    {
        return new CommandActivity()
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
    public void CommandActivity_Props()
    {
        var activity = SetupCommandActivity();


        Assert.NotNull(activity.ToCommand());

        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.CommandActivity' to type 'Microsoft.Teams.Api.Activities.ConversationUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToConversationUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void CommandActivity_JsonSerialize()
    {
        var activity = SetupCommandActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Command/CommandActivity.json"
        ), json);
    }

    [Fact]
    public void CommandActivity_JsonSerialize_Derived()
    {
        Activity activity = SetupCommandActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Command/CommandActivity.json"
        ), json);
    }

    [Fact]
    public void CommandActivity_JsonSerialize_Derived_Interface()
    {
        IActivity activity = SetupCommandActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Command/CommandActivity.json"
        ), json);
    }

    [Fact]
    public void CommandActivity_JsonSerialize_Interface_Derived()
    {
        IActivity activity = SetupCommandActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Command/CommandActivity.json"
        ), json);
    }


    [Fact]
    public void CommandActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Command/CommandActivity.json");
        var activity = JsonSerializer.Deserialize<CommandActivity>(json);
        var expected = SetupCommandActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToCommand());
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.CommandActivity' to type 'Microsoft.Teams.Api.Activities.ConversationUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToConversationUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }


    [Fact]
    public void CommandActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Command/CommandActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = SetupCommandActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToCommand());
        var expectedSubmitException = "Unable to cast object of type 'Microsoft.Teams.Api.Activities.CommandActivity' to type 'Microsoft.Teams.Api.Activities.InstallUpdateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToInstallUpdate());
        Assert.Equal(expectedSubmitException, ex.Message);
    }
}