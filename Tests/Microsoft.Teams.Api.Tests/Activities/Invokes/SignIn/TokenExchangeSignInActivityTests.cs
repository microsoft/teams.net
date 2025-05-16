using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.SignIn;

using static Microsoft.Teams.Api.Activities.Invokes.SignIn;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes;

public class TokenExchangeSignInActivityTests
{
    private TokenExchangeActivity SetupSignInTokenExchangeActivity()
    {
        return new TokenExchangeActivity()
        {
            Value = new ExchangeToken()
            {
                Id = "tokenExchangeId",
                ConnectionName = "connection Name",
                Token = "token"
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
    public void SignInTokenExchangeActivity_JsonSerialize()
    {
        var activity = SetupSignInTokenExchangeActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.Signin/tokenExchange";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.NotNull(activity.ToTokenExchange());
        var expectedSubmitException = "Unable to cast object of type 'TokenExchangeActivity' to type 'VerifyStateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToVerifyState());
        Assert.Equal(expectedSubmitException, ex.Message);
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/SignInTokenExchangeActivity.json"
        ), json);
    }

    [Fact]
    public void SignInTokenExchangeActivity_JsonSerialize_Derived()
    {
        SignInActivity activity = SetupSignInTokenExchangeActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.Signin/tokenExchange";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/SignInTokenExchangeActivity.json"
        ), json);
    }

    [Fact]
    public void SignInTokenExchangeActivity_JsonSerialize_Derived_Interface()
    {
        InvokeActivity activity = SetupSignInTokenExchangeActivity();

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        string expectedPath = "Activity.Invoke.Signin/tokenExchange";
        Assert.Equal(expectedPath, activity.GetPath());
        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Invokes/SignInTokenExchangeActivity.json"
        ), json);
    }

    [Fact]
    public void SignInTokenExchangeActivity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SignInTokenExchangeActivity.json");
        var activity = JsonSerializer.Deserialize<TokenExchangeActivity>(json);
        var expected = SetupSignInTokenExchangeActivity();

        Assert.Equal(expected.ToString(), activity!.ToString());
        Assert.NotNull(activity.ToTokenExchange());

        var expectedSubmitException = "Unable to cast object of type 'TokenExchangeActivity' to type 'VerifyStateActivity'.";
        var ex = Assert.Throws<System.InvalidCastException>(() => activity.ToVerifyState());
        Assert.Equal(expectedSubmitException, ex.Message);
    }

    [Fact]
    public void SignInTokenExchangeActivity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SignInTokenExchangeActivity.json");
        var activity = JsonSerializer.Deserialize<SignInActivity>(json);
        var expected = SetupSignInTokenExchangeActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
        Assert.NotNull(activity.ToSignIn());

    }

    [Fact]
    public void SignInTokenExchangeActivity_JsonDeserialize_Derived_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SignInTokenExchangeActivity.json");
        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);
        var expected = SetupSignInTokenExchangeActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }

    [Fact]
    public void SignInTokenExchangeActivity_JsonDeserialize_Derived_Activity_Interface()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/SignInTokenExchangeActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = SetupSignInTokenExchangeActivity();

        Assert.NotNull(activity);
        Assert.Equal(expected.ToString(), activity.ToString());
    }
}