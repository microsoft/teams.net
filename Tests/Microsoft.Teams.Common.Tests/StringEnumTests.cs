using System.Text.Json;

namespace Microsoft.Teams.Common.Tests;

public class StringEnumTests
{
    [Fact]
    public void JsonSerialize()
    {
        var value = new StringEnum("test");
        var json = JsonSerializer.Serialize(value);

        Assert.Equal("\"test\"", json);

        var obj = new Dictionary<string, object>()
        {
            { "hello", new StringEnum("world") }
        };

        json = JsonSerializer.Serialize(obj);
        Assert.Equal("{\"hello\":\"world\"}", json);
    }
}