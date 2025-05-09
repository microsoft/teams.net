using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;


using static Microsoft.Teams.Api.Activities.Invokes.MessageExtensions;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes;

public class AnonQueryLinkMEActivityTests
{
    private AnonQueryLinkActivity setupAnonQueryLinkActivity()
    {
        return new AnonQueryLinkActivity()
        {
            Value = new AppBasedQueryLink()
            {
                Url = "https://some-url"
            },
        };
    }

    [Fact]
    public void AnonQueryLinkMEActivity_JsonSerialize()
    {
        var activity = setupAnonQueryLinkActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/anonymousQueryLink";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/AnonQueryLinkMEActivity.json"
        ), json);
    }

    [Fact]
    public void AnonQueryLinkMEActivity_JsonSerialize_Derived()
    {
        MessageExtensionActivity activity = setupAnonQueryLinkActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/anonymousQueryLink";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/AnonQueryLinkMEActivity.json"
        ), json);
    }

    [Fact]
    public void AnonQueryLinkMEActivity_JsonSerialize_Derived_Interface()
    {
        InvokeActivity activity = setupAnonQueryLinkActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.ComposeExtension/anonymousQueryLink";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/AnonQueryLinkMEActivity.json"
        ), json);
    }

    [Fact]
    public void AnonQueryLinkMEActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/AnonQueryLinkMEActivity.json");
        var activity = JsonSerializer.Deserialize<AnonQueryLinkActivity>(json);
        var expected = setupAnonQueryLinkActivity();

        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());
        string expectedPath = "Activity.Invoke.ComposeExtension/anonymousQueryLink";
        Assert.Equal(expectedPath, activity.GetPath());
    }

    [Fact]
    public void AnonQueryLinkMEActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/AnonQueryLinkMEActivity.json");
        var activity = JsonSerializer.Deserialize<MessageExtensionActivity>(json);
        var expected = setupAnonQueryLinkActivity();

        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());
        var expectedSubmitException = "Unable to cast object of type 'AnonQueryLinkActivity' to type 'Microsoft.Teams.Api.Activities.Invokes.TaskActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToTask());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void AnonQueryLinkMEActivity_JsonDeserialize_Derived_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/AnonQueryLinkMEActivity.json");
        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);
        var expected = setupAnonQueryLinkActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToMessageExtension());

        var expectedSubmitException = "Unable to cast object of type 'AnonQueryLinkActivity' to type 'Microsoft.Teams.Api.Activities.Invokes.SignInActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToSignIn());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void AnonQueryLinkMEActivity_JsonDeserialize_Derived_Activity_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/AnonQueryLinkMEActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = setupAnonQueryLinkActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
        string expectedPath = "Activity.Invoke.ComposeExtension/anonymousQueryLink";
        Assert.Equal(expectedPath, activity.GetPath());
    }
}
