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

    [Fact]
    public void setupSignInVerifyStateActivity_JsonDeserialize_StateAsObject()
    {
        // Test JSON with state as an object (Android/iOS scenario)
        var jsonWithObjectState = @"{
  ""type"": ""invoke"",
  ""channelId"": ""msteams"",
  ""name"": ""signin/verifyState"",
  ""value"": {
    ""state"": {
      ""token"": ""abc123"",
      ""userId"": ""user123""
    }
  },
  ""from"": {
    ""id"": ""botId"",
    ""aadObjectId"": ""aadObjectId"",
    ""name"": ""User Name""
  },
  ""recipient"": {
    ""id"": ""recipientId"",
    ""name"": ""Recipient Name""
  },
  ""conversation"": {
    ""id"": ""conversationId"",
    ""conversationType"": ""groupChat""
  }
}";

        var activity = JsonSerializer.Deserialize<VerifyStateActivity>(jsonWithObjectState);

        Assert.NotNull(activity);
        Assert.NotNull(activity.Value);
        Assert.NotNull(activity.Value.State);
        
        // Verify the state was serialized to a JSON string
        Assert.Contains("token", activity.Value.State);
        Assert.Contains("abc123", activity.Value.State);
        Assert.Contains("userId", activity.Value.State);
        Assert.Contains("user123", activity.Value.State);
    }

    [Fact]
    public void setupSignInVerifyStateActivity_JsonDeserialize_StateAsObject_ViaSignInActivity()
    {
        // Test JSON with state as an object through SignInActivity
        var jsonWithObjectState = @"{
  ""type"": ""invoke"",
  ""channelId"": ""msteams"",
  ""name"": ""signin/verifyState"",
  ""value"": {
    ""state"": {
      ""sessionId"": ""session-456"",
      ""redirectUrl"": ""https://example.com/callback""
    }
  },
  ""from"": {
    ""id"": ""botId"",
    ""aadObjectId"": ""aadObjectId"",
    ""name"": ""User Name""
  },
  ""recipient"": {
    ""id"": ""recipientId"",
    ""name"": ""Recipient Name""
  },
  ""conversation"": {
    ""id"": ""conversationId"",
    ""conversationType"": ""groupChat""
  }
}";

        var activity = JsonSerializer.Deserialize<SignInActivity>(jsonWithObjectState);

        Assert.NotNull(activity);
        var verifyStateActivity = activity.ToVerifyState();
        Assert.NotNull(verifyStateActivity);
        Assert.NotNull(verifyStateActivity.Value.State);
        
        // Verify the state was serialized to a JSON string
        Assert.Contains("sessionId", verifyStateActivity.Value.State);
        Assert.Contains("session-456", verifyStateActivity.Value.State);
    }

    [Fact]
    public void setupSignInVerifyStateActivity_JsonDeserialize_StateAsObject_ViaActivity()
    {
        // Test JSON with state as an object through Activity
        var jsonWithObjectState = @"{
  ""type"": ""invoke"",
  ""channelId"": ""msteams"",
  ""name"": ""signin/verifyState"",
  ""value"": {
    ""state"": {
      ""code"": ""auth-code-789""
    }
  },
  ""from"": {
    ""id"": ""botId"",
    ""aadObjectId"": ""aadObjectId"",
    ""name"": ""User Name""
  },
  ""recipient"": {
    ""id"": ""recipientId"",
    ""name"": ""Recipient Name""
  },
  ""conversation"": {
    ""id"": ""conversationId"",
    ""conversationType"": ""groupChat""
  }
}";

        var activity = JsonSerializer.Deserialize<Activity>(jsonWithObjectState);

        Assert.NotNull(activity);
        Assert.True(activity is InvokeActivity);
        var invokeActivity = (InvokeActivity)activity;
        Assert.True(invokeActivity is SignInActivity);
        var signInActivity = (SignInActivity)invokeActivity;
        var verifyStateActivity = signInActivity.ToVerifyState();
        
        Assert.NotNull(verifyStateActivity.Value.State);
        Assert.Contains("code", verifyStateActivity.Value.State);
        Assert.Contains("auth-code-789", verifyStateActivity.Value.State);
    }
}