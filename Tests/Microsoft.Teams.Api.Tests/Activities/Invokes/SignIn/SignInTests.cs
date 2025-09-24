using System.Text.Json;

using Microsoft.Teams.Api.Activities.Invokes;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes.SignIn;

public class SignInTests
{
    private static SignInActivity? Deserialize(string json) => JsonSerializer.Deserialize<SignInActivity>(json);

    [Fact]
    public void SignIn_MissingName_Throws()
    {
        var json = "{\"type\":\"invoke\"}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("must have a 'name'", ex.Message);
    }

    [Fact]
    public void SignIn_NullName_Throws()
    {
        var json = "{\"type\":\"invoke\",\"name\":null}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("failed to deserialize invoke activity 'name' property", ex.Message);
    }

    [Fact]
    public void SignIn_UnknownName_Throws()
    {
        var json = "{\"type\":\"invoke\",\"name\":\"signin/other\"}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("doesn't match any known types", ex.Message);
    }
}