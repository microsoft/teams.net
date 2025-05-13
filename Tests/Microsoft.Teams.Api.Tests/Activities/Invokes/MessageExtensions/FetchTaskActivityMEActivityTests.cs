using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.MessageExtensions;

using static Microsoft.Teams.Api.Activities.Invokes.MessageExtensions;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes;

public class FetchTaskActivityMEActivityTests
{
    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static FetchTaskActivity SetupFetchTaskActivity()
    {
        return new FetchTaskActivity()
        {
            Value = new MessageExtensions.Action()
            {
                CommandContext = Commands.Context.Compose,
                CommandId = "commandId",
                BotMessagePreviewAction = MessagePreviewAction.Send,
                MessagePayload = new Messages.Message()
                {
                    Id = "messageId",
                    From = new Messages.From(),
                    Subject = "subject",
                    Body = new Messages.Body()
                    {
                        ContentType = new ContentType("text"),
                        Content = "content",
                    },
                },
                Context = new TaskModules.RequestContext()
                {
                    Theme = "dark-theme",
                },
                Data = new Dictionary<string, object>()
                {
                    { "key1", "value1" },
                    { "key2", "value2" }
                },
            }
        };
    }

    [Fact]
    public void FetchTaskMEActivity_JsonSerialize()
    {
        var activity = SetupFetchTaskActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Invoke.ComposeExtension/fetchTask";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/FetchTaskMEActivity.json"
        ), json);
    }

    [Fact]
    public void FetchTaskMEActivity_JsonSerialize_Derived()
    {
        MessageExtensionActivity activity = SetupFetchTaskActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Invoke.ComposeExtension/fetchTask";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/FetchTaskMEActivity.json"
        ), json);
    }

    [Fact]
    public void FetchTaskMEActivity_JsonSerialize_Derived_Interface()
    {
        InvokeActivity activity = SetupFetchTaskActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Invoke.ComposeExtension/fetchTask";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/FetchTaskMEActivity.json"
        ), json);
    }

    [Fact]
    public void FetchTaskMEActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/FetchTaskMEActivity.json");
        var activity = JsonSerializer.Deserialize<FetchTaskActivity>(json, CachedJsonSerializerOptions);
        var expected = SetupFetchTaskActivity();
        Assert.Equal(expected.ToString(), activity!.ToString());

        var expectedSubmitException = "Unable to cast object of type 'FetchTaskActivity' to type 'Microsoft.Teams.Api.Activities.Invokes.TaskActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToTask());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void FetchTaskMEActivityJsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/FetchTaskMEActivity.json");
        var activity = JsonSerializer.Deserialize<MessageExtensionActivity>(json, CachedJsonSerializerOptions);
        var expected = SetupFetchTaskActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }

    [Fact]
    public void FetchTaskMEActivity_JsonDeserialize_Derived_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/FetchTaskMEActivity.json");
        var activity = JsonSerializer.Deserialize<InvokeActivity>(json, CachedJsonSerializerOptions);
        var expected = SetupFetchTaskActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }

    [Fact]
    public void FetchTaskMEActivity_JsonDeserialize_Derived_Activity_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/FetchTaskMEActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json, CachedJsonSerializerOptions);
        var expected = SetupFetchTaskActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }
}