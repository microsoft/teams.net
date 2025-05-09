using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.Entities;
using Microsoft.Teams.Api.MessageExtensions;

using static Microsoft.Teams.Api.Activities.Invokes.MessageExtensions;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes;

public class QueryMEActivityTests
{
    private QueryActivity setupQueryActivity()
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

        return new QueryActivity()
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
    public void QueryMEActivity_JsonSerialize()
    {
        var activity = setupQueryActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/query";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/QueryMEActivity.json"
        ), json);
    }

    [Fact]
    public void QueryMEActivity_JsonSerialize_Derived()
    {
        MessageExtensionActivity activity = setupQueryActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/query";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/QueryMEActivity.json"
        ), json);
    }

    [Fact]
    public void QueryMEActivity_JsonSerialize_Derived_Interface()
    {
        InvokeActivity activity = setupQueryActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/query";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/QueryMEActivity.json"
        ), json);
    }

    [Fact]
    public void QueryMEActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/QueryMEActivity.json");
        var activity = JsonSerializer.Deserialize<QueryActivity>(json);
        var expected = setupQueryActivity();

        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());
    }

    [Fact]
    public void QueryMEActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/QueryMEActivity.json");
        var activity = JsonSerializer.Deserialize<MessageExtensionActivity>(json);
        var expected = setupQueryActivity();

        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());
        var expectedSubmitException = "Unable to cast object of type 'QueryActivity' to type 'Microsoft.Teams.Api.Activities.Invokes.TaskActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToTask());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void QueryMEActivity_JsonDeserialize_Derived_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/QueryMEActivity.json");
        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);
        var expected = setupQueryActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());
    }

    [Fact]
    public void QueryMEActivity_JsonDeserialize_Derived_Activity_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/QueryMEActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = setupQueryActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }
}
