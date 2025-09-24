using System.Text.Json;

using Microsoft.Teams.Api.Activities.Invokes;

namespace Microsoft.Teams.Api.Tests.Activities.Invokes.MessageInvokes; // keep adjusted namespace

public class MessagesTests
{
    private static MessageActivity? Deserialize(string json) => JsonSerializer.Deserialize<MessageActivity>(json);

    [Fact]
    public void Message_MissingName_Throws()
    {
        var json = "{\"type\":\"invoke\"}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("must have a 'name'", ex.Message);
    }

    [Fact]
    public void Message_NullName_Throws()
    {
        var json = "{\"type\":\"invoke\",\"name\":null}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("failed to deserialize invoke activity 'name' property", ex.Message);
    }

    [Fact]
    public void Message_UnknownName_Throws()
    {
        var json = "{\"type\":\"invoke\",\"name\":\"message/other\"}";
        var ex = Assert.Throws<JsonException>(() => Deserialize(json));
        Assert.Contains("doesn't match any known types", ex.Message);
    }
}