using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;

using static Microsoft.Teams.Api.Activities.Invokes.MessageExtensions;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes.MessageExtensions;

public class MessageExtensionsTests
{
    private static MessageExtensionActivity? Deserialize(string json) => JsonSerializer.Deserialize<MessageExtensionActivity>(json);

    [Fact]
    public void MessageExtension_MissingName_Throws()
    {
        var json = "{\"type\":\"invoke\"}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("must have a 'name'", ex.Message);
    }

    [Fact]
    public void MessageExtension_NullName_Throws()
    {
        var json = "{\"type\":\"invoke\",\"name\":null}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("failed to deserialize invoke activity 'name' property", ex.Message);
    }

    [Fact]
    public void MessageExtension_UnknownName_Throws()
    {
        var json = "{\"type\":\"invoke\",\"name\":\"composeExtension/other\"}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("doesn't match any known types", ex.Message);
    }

    [Fact]
    public void MessageExtension_Query_Value_AccessibleFromDerivedType()
    {
        var json = "{\"type\":\"invoke\",\"name\":\"composeExtension/query\",\"value\":{\"commandId\":\"searchCmd\"}}";
        MessageExtensionActivity? activity = Deserialize(json);
        var query = Assert.IsType<QueryActivity>(activity);
        Assert.NotNull(query.Value);
        Assert.Equal("searchCmd", query.Value.CommandId);
    }

    [Fact]
    public void MessageExtension_Query_Value_AccessibleFromBaseType()
    {
        var json = "{\"type\":\"invoke\",\"name\":\"composeExtension/query\",\"value\":{\"commandId\":\"searchCmd\"}}";
        var activity = Deserialize(json);
        var invoke = Assert.IsAssignableFrom<InvokeActivity>(activity);
        Assert.NotNull(invoke.Value);
    }
}