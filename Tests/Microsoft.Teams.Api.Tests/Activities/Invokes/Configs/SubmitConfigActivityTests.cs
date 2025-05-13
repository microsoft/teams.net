using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;

using static Microsoft.Teams.Api.Activities.Invokes.Configs;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes;

public class ConfigSubmitActivityTests
{
    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private SubmitActivity SetupConfigSubmitActivity()
    {
        var value = "You have chosen to submit config for bot";
        return new SubmitActivity(value);
    }

    [Fact]
    public void ConfigSubmitActivity_JsonSerialize()
    {
        var activity = SetupConfigSubmitActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Invoke.Config/submit";
        Assert.Equal(expectedPath, activity.GetPath());

        Assert.NotNull(activity.ToSubmit());
        var expectedSubmitException = "Unable to cast object of type 'SubmitActivity' to type 'FetchActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToFetch());
        Assert.Equal(expectedSubmitException, ex.Message);
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/ConfigSubmitActivity.json"
        ), json);
    }

    [Fact]
    public void ConfigSubmitActivity_JsonSerialize_Derived()
    {
        ConfigActivity activity = SetupConfigSubmitActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Invoke.Config/submit";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/ConfigSubmitActivity.json"
        ), json);
    }

    [Fact]
    public void ConfigSubmitActivity_JsonSerialize_Derived_Interface()
    {
        InvokeActivity activity = SetupConfigSubmitActivity();

        var json = JsonSerializer.Serialize(activity, CachedJsonSerializerOptions);

        string expectedPath = "Activity.Invoke.Config/submit";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/ConfigSubmitActivity.json"
        ), json);
    }

    [Fact]
    public void ConfigSubmitActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/ConfigSubmitActivity.json");
        var activity = JsonSerializer.Deserialize<SubmitActivity>(json, CachedJsonSerializerOptions);
        var expected = SetupConfigSubmitActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToConfig());

        var expectedSubmitException = "Unable to cast object of type 'SubmitActivity' to type 'Microsoft.Teams.Api.Activities.Invokes.TaskActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToTask());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void ConfigSubmitActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/ConfigSubmitActivity.json");
        var activity = JsonSerializer.Deserialize<ConfigActivity>(json, CachedJsonSerializerOptions);
        var expected = SetupConfigSubmitActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToSubmit());
        var expectedSubmitException = "Unable to cast object of type 'SubmitActivity' to type 'FetchActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToFetch());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void ConfigSubmitActivity_JsonDeserialize_Derived_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/ConfigSubmitActivity.json");
        var activity = JsonSerializer.Deserialize<InvokeActivity>(json, CachedJsonSerializerOptions);
        var expected = SetupConfigSubmitActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }

    [Fact]
    public void ConfigSubmitActivity_JsonDeserialize_Derived_Activity_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/ConfigSubmitActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json, CachedJsonSerializerOptions);
        var expected = SetupConfigSubmitActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }
}