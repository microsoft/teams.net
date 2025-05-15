using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.Entities;
using Microsoft.Teams.Api.MessageExtensions;

using static Microsoft.Teams.Api.Activities.Invokes.MessageExtensions;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes;

public class SelectItemMEActivityTests
{
    private SelectItemActivity SetupSelectItemActivity()
    {
        IList<IEntity> _entityList =
        [
            new ClientInfoEntity()
            {
                Platform = "Windows",
                Locale = "en-US",
                Country = "US",
                Timezone = "GMT-8",
            }
        ];

        return new SelectItemActivity()
        {
            Value = new Query()
            {
                CommandId = "selectCmd",
                Parameters = new List<Parameter>()
                {
                    new Parameter()
                    {
                        Name = "Somelist",
                        Value = "Toronto"
                    }
                },
            },
            Conversation = new Api.Conversation()
            {
                Id = "convId",
                Type = ConversationType.Personal
            },
            Id = "f:622749630322482883",
            ServiceUrl = "https://me-url",
            From = new Account()
            {
                Id = "botId",
                Name = "User Name",
                AadObjectId = "aadObjectId"
            },
            Recipient = new Account()
            {
                Id = "recipientId",
                Name = "Recipient Name",
            },
            Entities = _entityList,
        };
    }

    [Fact]
    public void SelectItemMEActivity_JsonSerialize()
    {
        var activity = SetupSelectItemActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/selectItem";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/SelectItemMEActivity.json"
        ), json);
    }

    [Fact]
    public void SelectItemMEActivity_JsonSerialize_Derived()
    {
        MessageExtensionActivity activity = SetupSelectItemActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/selectItem";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/SelectItemMEActivity.json"
        ), json);
    }

    [Fact]
    public void SelectItemMEActivity_JsonSerialize_Derived_Interface()
    {
        InvokeActivity activity = SetupSelectItemActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/selectItem";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/SelectItemMEActivity.json"
        ), json);
    }

    [Fact]
    public void SelectItemMEActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SelectItemMEActivity.json");
        var activity = JsonSerializer.Deserialize<SelectItemActivity>(json);
        var expected = SetupSelectItemActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToMessageExtension());
    }

    [Fact]
    public void SelectItemMEActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SelectItemMEActivity.json");
        var activity = JsonSerializer.Deserialize<MessageExtensionActivity>(json);
        var expected = SetupSelectItemActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToMessageExtension());
        var expectedSubmitException = "Unable to cast object of type 'SelectItemActivity' to type 'Microsoft.Teams.Api.Activities.Invokes.TaskActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToTask());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void SelectItemMEActivity_JsonDeserialize_Derived_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SelectItemMEActivity.json");
        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);
        var expected = SetupSelectItemActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());
    }

    [Fact]
    public void SelectItemMEActivity_JsonDeserialize_Derived_Activity_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SelectItemMEActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = SetupSelectItemActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }
}