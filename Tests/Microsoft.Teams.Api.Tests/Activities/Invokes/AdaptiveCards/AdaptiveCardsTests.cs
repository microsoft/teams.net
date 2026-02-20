using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;

using static Microsoft.Teams.Api.Activities.Invokes.AdaptiveCards;

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

    [Fact]
    public void AdaptiveCard_Action_Value_AccessibleFromDerivedType()
    {
        var json = "{\"type\":\"invoke\",\"name\":\"adaptiveCard/action\",\"value\":{\"action\":{\"type\":\"Action.Submit\"}}}";
        var activity = Deserialize(json);
        var action = Assert.IsType<ActionActivity>(activity);
        Assert.NotNull(action.Value);
        Assert.NotNull(action.Value.Action);
    }

    [Fact]
    public void AdaptiveCard_Action_Value_AccessibleFromBaseType()
    {
        var json = "{\"type\":\"invoke\",\"name\":\"adaptiveCard/action\",\"value\":{\"action\":{\"type\":\"Action.Submit\"}}}";
        var activity = Deserialize(json);
        var invoke = Assert.IsAssignableFrom<InvokeActivity>(activity);
        Assert.NotNull(invoke.Value);
    }
}