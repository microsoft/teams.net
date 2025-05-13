using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;


using static Microsoft.Teams.Api.Activities.Invokes.MessageExtensions;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes;

public class QueryLinkMEActivityTests
{
    private QueryLinkActivity setupQueryLinkActivity()
    {
        return new QueryLinkActivity()
        {
            Value = new AppBasedQueryLink()
            {
                Url = "https://some-url"
            },
        };
    }

    [Fact]
    public void QueryLinkMEActivity_JsonSerialize()
    {
        var activity = setupQueryLinkActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/queryLink";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/QueryLinkMEActivity.json"
        ), json);
    }

    [Fact]
    public void QueryLinkMEActivity_JsonSerialize_Derived()
    {
        MessageExtensionActivity activity = setupQueryLinkActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/queryLink";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/QueryLinkMEActivity.json"
        ), json);
    }

    [Fact]
    public void QueryLinkMEActivity_JsonSerialize_Derived_Interface()
    {
        InvokeActivity activity = setupQueryLinkActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/queryLink";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/QueryLinkMEActivity.json"
        ), json);
    }

    [Fact]
    public void QueryLinkMEActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/QueryLinkMEActivity.json");
        var activity = JsonSerializer.Deserialize<QueryLinkActivity>(json);
        var expected = setupQueryLinkActivity();

        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());
    }

    [Fact]
    public void QueryLinkMEActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/QueryLinkMEActivity.json");
        var activity = JsonSerializer.Deserialize<MessageExtensionActivity>(json);
        var expected = setupQueryLinkActivity();

        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());
        var expectedSubmitException = "Unable to cast object of type 'QueryLinkActivity' to type 'Microsoft.Teams.Api.Activities.Invokes.TaskActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToTask());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void QueryLinkMEActivity_JsonDeserialize_Derived_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/QueryLinkMEActivity.json");
        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);
        var expected = setupQueryLinkActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());

        var expectedSubmitException = "Unable to cast object of type 'QueryLinkActivity' to type 'Microsoft.Teams.Api.Activities.Invokes.SignInActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToSignIn());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void QueryLinkMEActivity_JsonDeserialize_Derived_Activity_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/QueryLinkMEActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = setupQueryLinkActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }
}