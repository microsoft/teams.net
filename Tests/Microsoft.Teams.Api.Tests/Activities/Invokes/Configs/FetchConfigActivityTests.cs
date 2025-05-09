using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;

using static Microsoft.Teams.Api.Activities.Invokes.Configs;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes;

public class ConfigFetchActivityTests
{
    private FetchActivity setupConfigFetchActivity()
    {
        var value = new Cards.HeroCard()
        {
            Title = "test card",
            SubTitle = "test fetch config activity"
        };
        return new FetchActivity(value);
    }

    [Fact]
    public void ConfigFetchActivity_JsonSerialize()
    {
        var activity = setupConfigFetchActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.Config/fetch";
        Assert.Equal(expectedPath, activity.GetPath());

        Assert.NotNull(activity.ToFetch());
        var expectedSubmitException = "Unable to cast object of type 'FetchActivity' to type 'SubmitActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToSubmit());
        Assert.Equal(expectedSubmitException, ex.Message);

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/ConfigFetchActivity.json"
        ), json);
    }

    [Fact]
    public void ConfigFetchActivity_JsonSerialize_Derived()
    {
        ConfigActivity activity = setupConfigFetchActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.Config/fetch";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/ConfigFetchActivity.json"
        ), json);
    }

    [Fact]
    public void ConfigFetchActivity_JsonSerialize_Derived_Interface()
    {
        InvokeActivity activity = setupConfigFetchActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        
        string expectedPath = "Activity.Invoke.Config/fetch";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/ConfigFetchActivity.json"
        ), json);
    }

    [Fact]
    public void ConfigFetchActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/ConfigFetchActivity.json");
        var activity = JsonSerializer.Deserialize<SubmitActivity>(json);
        var expected = setupConfigFetchActivity();

        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToConfig());

        var expectedSubmitException = "Unable to cast object of type 'SubmitActivity' to type 'Microsoft.Teams.Api.Activities.Invokes.TaskActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToTask());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void ConfigFetchActivityJsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/ConfigFetchActivity.json");
        var activity = JsonSerializer.Deserialize<ConfigActivity>(json);
        var expected = setupConfigFetchActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToFetch());
        var expectedSubmitException = "Unable to cast object of type 'FetchActivity' to type 'SubmitActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToSubmit());
    }

    [Fact]
    public void ConfigFetchActivity_JsonDeserialize_Derived_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/ConfigFetchActivity.json");
        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);
        var expected = setupConfigFetchActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());

    }

    [Fact]
    public void ConfigFetchActivity_JsonDeserialize_Derived_Activity_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/ConfigFetchActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = setupConfigFetchActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }
}
