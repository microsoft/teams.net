using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.Entities;
using Microsoft.Teams.Api.MessageExtensions;

using static Microsoft.Teams.Api.Activities.Invokes.MessageExtensions;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes;

public class QuerySettingsUrlMEActivityTests
{
    private QuerySettingsUrlActivity setupQuerySettingsUrlActivity()
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
        return new QuerySettingsUrlActivity()
        {
            Value = new Query()
            {
                CommandId = "searchCmd",
                Parameters = new List<Parameter>()
                {
                    new Parameter()
                    {
                        Name = "Somelist",
                        Value = "Toronto"
                    }
                },
            },
            Conversation = new Conversation()
            {
                Id = "convId",
                Type = ConversationType.Personal
            },
            Id = "id:data",
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
    public void QuerySettingsUrlMEActivity_JsonSerialize()
    {
        var activity = setupQuerySettingsUrlActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/querySettingsUrl";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/QuerySettingsUrlMEActivity.json"
        ), json);
    }

    [Fact]
    public void QuerySettingsUrlMEActivity_JsonSerialize_Derived()
    {
        MessageExtensionActivity activity = setupQuerySettingsUrlActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/querySettingsUrl";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/QuerySettingsUrlMEActivity.json"
        ), json);
    }

    [Fact]
    public void QuerySettingsUrlMEActivity_JsonSerialize_Derived_Interface()
    {
        InvokeActivity activity = setupQuerySettingsUrlActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/querySettingsUrl";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/QuerySettingsUrlMEActivity.json"
        ), json);
    }

    [Fact]
    public void QuerySettingsUrlMEActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/QuerySettingsUrlMEActivity.json");
        var activity = JsonSerializer.Deserialize<QuerySettingsUrlActivity>(json);
        var expected = setupQuerySettingsUrlActivity();

        string expectedPath = "Activity.Invoke.ComposeExtension/querySettingsUrl";
        Assert.Equal(expectedPath, activity.GetPath());

        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());
    }

    [Fact]
    public void QuerySettingsUrlMEActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/QuerySettingsUrlMEActivity.json");
        var activity = JsonSerializer.Deserialize<MessageExtensionActivity>(json);
        var expected = setupQuerySettingsUrlActivity();

        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());
        var expectedSubmitException = "Unable to cast object of type 'QuerySettingsUrlActivity' to type 'Microsoft.Teams.Api.Activities.Invokes.TaskActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToTask());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void QuerySettingsUrlMEActivity_JsonDeserialize_Derived_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/QuerySettingsUrlMEActivity.json");
        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);
        var expected = setupQuerySettingsUrlActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());

        var expectedSubmitException = "Unable to cast object of type 'QuerySettingsUrlActivity' to type 'Microsoft.Teams.Api.Activities.Invokes.SignInActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToSignIn());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void QuerySettingsUrlMEActivity_JsonDeserialize_Derived_Activity_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/QuerySettingsUrlMEActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = setupQuerySettingsUrlActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }
}