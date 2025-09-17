using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.Entities;
using Microsoft.Teams.Api.MessageExtensions;

using static Microsoft.Teams.Api.Activities.Invokes.MessageExtensions;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes;

public class QuerySettingsUrlMEActivityTests
{
    private QuerySettingsUrlActivity SetupQuerySettingsUrlActivity()
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
            Conversation = new Api.Conversation()
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
        var activity = SetupQuerySettingsUrlActivity();

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
        MessageExtensionActivity activity = SetupQuerySettingsUrlActivity();

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
        InvokeActivity activity = SetupQuerySettingsUrlActivity();

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
        var expected = SetupQuerySettingsUrlActivity();

        string expectedPath = "Activity.Invoke.ComposeExtension/querySettingsUrl";
        Assert.Equal(expectedPath, activity!.GetPath());

        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());
    }

    [Fact]
    public void QuerySettingsUrlMEActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/QuerySettingsUrlMEActivity.json");
        var activity = JsonSerializer.Deserialize<MessageExtensionActivity>(json);
        var expected = SetupQuerySettingsUrlActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
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
        var expected = SetupQuerySettingsUrlActivity();

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
        var expected = SetupQuerySettingsUrlActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }

    [Fact]
    public void QuerySettingsUrlMEActivity_JsonDeserialize_RealWorldData()
    {
        var json = """
        {"name":"composeExtension/querySettingUrl","type":"invoke","timestamp":"2025-09-17T22:17:56.791Z","localTimestamp":"2025-09-17T15:17:56.791-07:00","id":"f:8ee2eb5b-6f53-54aa-2726-057e73a4da65","channelId":"msteams","serviceUrl":"https://smba.trafficmanager.net/amer/50612dbb-0237-4969-b378-8d42590f9c00/","from":{"id":"29:1_GIvHPvI3atQPvgLSHXVsZUwNN_c0FRgZQx6xtFAe_cZZW3uJ8VZp5x6Kl1DdRjnmsFg9x7wKQ83eXcrOLGIXw","name":"Aamir Jawaid","aadObjectId":"1f41a2a6-addd-4719-b075-28eb1c7a66f4"},"conversation":{"conversationType":"personal","tenantId":"50612dbb-0237-4969-b378-8d42590f9c00","id":"a:1KKKvq79q7mKR5h1D1SZtDDOCGQmeToAQXPLOAd4T9K3ineZ38Nwm9ELsV6Bv9yeRsw9taGd-byCJNBaaiy9_4u3bl0cuVok7IWxAAOmlH12adkr4u1lBiyye_wc1FwNu"},"recipient":{"id":"28:c083fa20-55e5-4aeb-916a-0cec47a40b62","name":"MessageExtensions"},"entities":[{"locale":"en-US","country":"US","platform":"Web","timezone":"America/Los_Angeles","type":"clientInfo"}],"channelData":{"tenant":{"id":"50612dbb-0237-4969-b378-8d42590f9c00"},"source":{"name":"compose"}},"value":{"commandId":"searchQuery","parameters":[{"name":"searchQuery","value":""}]},"locale":"en-US","localTimezone":"America/Los_Angeles"}
        """;
        
        var activity = JsonSerializer.Deserialize<Activity>(json);
        
        Assert.NotNull(activity);
        Assert.Equal("invoke", activity.Type.ToString());
        Assert.Equal("f:8ee2eb5b-6f53-54aa-2726-057e73a4da65", activity.Id);
        Assert.Equal("msteams", activity.ChannelId.ToString());
        Assert.Equal("29:1_GIvHPvI3atQPvgLSHXVsZUwNN_c0FRgZQx6xtFAe_cZZW3uJ8VZp5x6Kl1DdRjnmsFg9x7wKQ83eXcrOLGIXw", activity.From.Id);
        Assert.Equal("Aamir Jawaid", activity.From.Name);
        Assert.Equal("1f41a2a6-addd-4719-b075-28eb1c7a66f4", activity.From.AadObjectId);
        Assert.Equal("28:c083fa20-55e5-4aeb-916a-0cec47a40b62", activity.Recipient.Id);
        Assert.Equal("MessageExtensions", activity.Recipient.Name);
    }
}