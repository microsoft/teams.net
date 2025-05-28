using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.MessageExtensions;

using static Microsoft.Teams.Api.Activities.Invokes.MessageExtensions;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes;

public class SubmitActionMEActivityTests
{
    private SubmitActionActivity SetupSubmitActionActivity()
    {
        return new SubmitActionActivity()
        {
            Value = new Api.MessageExtensions.Action()
            {
                CommandContext = Commands.Context.Message,
                CommandId = "commandId",
                BotMessagePreviewAction = MessagePreviewAction.Edit,
                MessagePayload = new Messages.Message()
                {
                    Id = "messageId",
                    From = new Messages.From(),
                    Subject = "subject",
                    Body = new Messages.Body()
                    {
                        ContentType = new ContentType("application/vnd.microsoft.card.adaptive"),
                        Content = "<adaptive card content json>",
                        TextContent = "text content",

                    },
                },
                Context = new TaskModules.RequestContext()
                {
                    Theme = "default",
                },
                Data = new Dictionary<string, object>()
                {
                    { "id", "submitButton" },
                    { "formField1", "formField1value" },
                    { "formField2", "formField2value" }
                },
            },
            Conversation = new Api.Conversation()
            {
                Id = "conversationId",
                Type = ConversationType.Personal
            },
        };
    }

    [Fact]
    public void SubmitActionMEActivity_JsonSerialize()
    {
        var activity = SetupSubmitActionActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/submitAction";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/SubmitActionMEActivity.json"
        ), json);
    }

    [Fact]
    public void SubmitActionMEActivity_JsonSerialize_Derived()
    {
        MessageExtensionActivity activity = SetupSubmitActionActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/submitAction";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/SubmitActionMEActivity.json"
        ), json);
    }

    [Fact]
    public void SubmitActionMEActivity_JsonSerialize_Derived_Interface()
    {
        InvokeActivity activity = SetupSubmitActionActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/submitAction";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/SubmitActionMEActivity.json"
        ), json);
    }

    [Fact]
    public void SubmitActionMEActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SubmitActionMEActivity.json");
        var activity = JsonSerializer.Deserialize<SubmitActionActivity>(json);
        var expected = SetupSubmitActionActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToMessageExtension());

        var expectedSubmitException = "Unable to cast object of type 'SubmitActionActivity' to type 'Microsoft.Teams.Api.Activities.Invokes.TaskActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToTask());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void SubmitActionMEActivityJsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SubmitActionMEActivity.json");
        var activity = JsonSerializer.Deserialize<MessageExtensionActivity>(json);
        var expected = SetupSubmitActionActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }

    [Fact]
    public void SubmitActionMEActivity_JsonDeserialize_Derived_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SubmitActionMEActivity.json");
        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);
        var expected = SetupSubmitActionActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }

    [Fact]
    public void SubmitActionMEActivity_JsonDeserialize_Derived_Activity_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SubmitActionMEActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = SetupSubmitActionActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }
}