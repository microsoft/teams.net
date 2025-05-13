using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.MessageExtensions;

using static Microsoft.Teams.Api.Activities.Invokes.MessageExtensions;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes;

public class SettingMEActivityTests
{
    private SettingActivity SetupSettingMEActivity()
    {
        return new SettingActivity()
        {
            Value = new Query()
            {
                CommandId = "commandId",
                Parameters =
                [
                    new Parameter()
                    {
                        Name = "parameter1",
                        Value = "value1"
                    },
                    new Parameter()
                    {
                        Name = "parameter2",
                        Value = "value2"
                    }
                ],
                QueryOptions = new Query.Options()
                {
                    Skip = 0,
                    Count = 10
                },
                State = "state"
            },
            Conversation = new Conversation()
            {
                Id = "conversationId",
                Type = ConversationType.GroupChat
            },
        };
    }

    [Fact]
    public void SettingMEActivity_JsonSerialize()
    {
        var activity = SetupSettingMEActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/setting";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/settingMEActivity.json"
        ), json);
    }

    [Fact]
    public void SettingMEActivity_JsonSerialize_Derived()
    {
        MessageExtensionActivity activity = SetupSettingMEActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/setting";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/SettingMEActivity.json"
        ), json);
    }

    [Fact]
    public void SettingMEActivity_JsonSerialize_Derived_Interface()
    {
        InvokeActivity activity = SetupSettingMEActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/setting";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/SettingMEActivity.json"
        ), json);
    }

    [Fact]
    public void SettingMEActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SettingMEActivity.json");
        var activity = JsonSerializer.Deserialize<SettingActivity>(json);
        var expected = SetupSettingMEActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToMessageExtension());


    }

    [Fact]
    public void SettingMEActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SettingMEActivity.json");
        var activity = JsonSerializer.Deserialize<MessageExtensionActivity>(json);
        var expected = SetupSettingMEActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToMessageExtension());
        var expectedSubmitException = "Unable to cast object of type 'SettingActivity' to type 'Microsoft.Teams.Api.Activities.Invokes.TaskActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToTask());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void SettingMEActivity_JsonDeserialize_Derived_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SettingMEActivity.json");
        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);
        var expected = SetupSettingMEActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());
    }

    [Fact]
    public void SettingMEActivity_JsonDeserialize_Derived_Activity_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SettingMEActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = SetupSettingMEActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }
}