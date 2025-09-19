using System.Text.Json;

using Microsoft.Teams.Api.Activities.Invokes;

using static Microsoft.Teams.Api.Activities.Invokes.Configs;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes.Configs;

public class ConfigsTests
{
    private static ConfigActivity? Deserialize(string json) => JsonSerializer.Deserialize<ConfigActivity>(json);

    [Theory]
    [InlineData("config/fetch", typeof(FetchActivity))]
    [InlineData("config/submit", typeof(SubmitActivity))]
    public void Config_Known(string name, Type expected)
    {
        var json = $"{{\"type\":\"invoke\",\"name\":\"{name}\"}}";
        var activity = Deserialize(json);
        Assert.NotNull(activity);
        Assert.Equal(expected, activity!.GetType());
        Assert.True(activity.Name.IsConfig);
    }

    [Fact]
    public void Config_MissingName_Throws()
    {
        var json = "{\"type\":\"invoke\"}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("must have a 'name'", ex.Message);
    }

    [Fact]
    public void Config_NullName_Throws()
    {
        var json = "{\"type\":\"invoke\",\"name\":null}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("failed to deserialize invoke activity 'name' property", ex.Message);
    }

    [Fact]
    public void Config_UnknownName_Throws()
    {
        var json = "{\"type\":\"invoke\",\"name\":\"config/other\"}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("doesn't match any known types", ex.Message);
    }
}