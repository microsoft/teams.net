using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.SignIn;

using static Microsoft.Teams.Api.Activities.Invokes.SignIn;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes;

public class VerifyStateSignInActivityTests
{
    private VerifyStateActivity SetupSignInValidStateActivity()
    {
        return new VerifyStateActivity()
        {
            Value = new StateVerifyQuery()
            {
                State = "success"
            },
            Conversation = new Conversation()
            {
                Id = "conversationId",
                Type = ConversationType.GroupChat
            },
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
        };
    }

    [Fact]
    public void setupSignInVerifyStateActivity_JsonSerialize()
    {
        var activity = SetupSignInValidStateActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.Signin/verifyState";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.NotNull(activity.ToVerifyState());
        var expectedSubmitException = "Unable to cast object of type 'VerifyStateActivity' to type 'TokenExchangeActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToTokenExchange());
        Assert.Equal(expectedSubmitException, ex.Message);
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/SignInVerifyStateActivity.json"
        ), json);
    }

    [Fact]
    public void setupSignInVerifyStateActivity_JsonSerialize_Derived()
    {
        SignInActivity activity = SetupSignInValidStateActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.Signin/verifyState";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/SignInVerifyStateActivity.json"
        ), json);
    }

    [Fact]
    public void setupSignInVerifyStateActivity_JsonSerialize_Derived_Interface()
    {
        InvokeActivity activity = SetupSignInValidStateActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.Signin/verifyState";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/SignInVerifyStateActivity.json"
        ), json);
    }

    [Fact]
    public void setupSignInVerifyStateActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SignInVerifyStateActivity.json");
        var activity = JsonSerializer.Deserialize<VerifyStateActivity>(json);
        var expected = SetupSignInValidStateActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToVerifyState());

        var expectedSubmitException = "Unable to cast object of type 'VerifyStateActivity' to type 'TokenExchangeActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToTokenExchange());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void setupSignInVerifyStateActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SignInVerifyStateActivity.json");
        var activity = JsonSerializer.Deserialize<SignInActivity>(json);
        var expected = SetupSignInValidStateActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToSignIn());

    }

    [Fact]
    public void setupSignInVerifyStateActivity_JsonDeserialize_Derived_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SignInVerifyStateActivity.json");
        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);
        var expected = SetupSignInValidStateActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }

    [Fact]
    public void setupSignInVerifyStateActivity_JsonDeserialize_Derived_Activity_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SignInVerifyStateActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = SetupSignInValidStateActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }
}