using System.Text.Json;

using Microsoft.Teams.Api.Activities.Invokes;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes.AdaptiveCards;

public class AdaptiveCardsTests
{
    private static AdaptiveCardActivity? Deserialize(string json) => JsonSerializer.Deserialize<AdaptiveCardActivity>(json);

    [Fact]
    public void AdaptiveCard_MissingName_Throws()
    {
        var json = "{\"type\":\"invoke\"}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("must have a 'name'", ex.Message);
    }

    [Fact]
    public void AdaptiveCard_NullName_Throws()
    {
        var json = "{\"type\":\"invoke\",\"name\":null}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("failed to deserialize invoke activity 'name' property", ex.Message);
    }

    [Fact]
    public void AdaptiveCard_UnknownName_Throws()
    {
        var json = "{\"type\":\"invoke\",\"name\":\"adaptiveCard/unknown\"}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("doesn't match any known types", ex.Message);
    }
}