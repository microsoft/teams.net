using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;

using static Microsoft.Teams.Api.Activities.Invokes.SignIn;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes;

public class FailureSignInActivityTests
{
    private FailureActivity SetupSignInFailureActivity()
    {
        return new FailureActivity()
        {
            Value = new Api.SignIn.Failure()
            {
                Code = "resourcematchfailed",
                Message = "Resource match failed"
            },
            Conversation = new Api.Conversation()
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
    public void SignInFailureActivity_JsonSerialize()
    {
        var activity = SetupSignInFailureActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.Signin/failure";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.NotNull(activity.ToFailure());
        var expectedSubmitException = "Unable to cast object of type 'FailureActivity' to type 'TokenExchangeActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToTokenExchange());
        Assert.Equal(expectedSubmitException, ex.Message);
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/SignInFailureActivity.json"
        ), json);
    }

    [Fact]
    public void SignInFailureActivity_JsonSerialize_Derived()
    {
        SignInActivity activity = SetupSignInFailureActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.Signin/failure";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/SignInFailureActivity.json"
        ), json);
    }

    [Fact]
    public void SignInFailureActivity_JsonSerialize_Derived_Interface()
    {
        InvokeActivity activity = SetupSignInFailureActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.Signin/failure";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/SignInFailureActivity.json"
        ), json);
    }

    [Fact]
    public void SignInFailureActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SignInFailureActivity.json");
        var activity = JsonSerializer.Deserialize<FailureActivity>(json);
        var expected = SetupSignInFailureActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToFailure());

        var expectedSubmitException = "Unable to cast object of type 'FailureActivity' to type 'TokenExchangeActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToTokenExchange());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void SignInFailureActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SignInFailureActivity.json");
        var activity = JsonSerializer.Deserialize<SignInActivity>(json);
        var expected = SetupSignInFailureActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToSignIn());
    }

    [Fact]
    public void SignInFailureActivity_JsonDeserialize_Derived_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SignInFailureActivity.json");
        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);
        var expected = SetupSignInFailureActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }

    [Fact]
    public void SignInFailureActivity_JsonDeserialize_Derived_Activity_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SignInFailureActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = SetupSignInFailureActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }
}
