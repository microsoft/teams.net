using System.Text.Json;

namespace Microsoft.Teams.Common.Tests;

public class UnionTests
{
    [Fact]
    public void JsonSerialize()
    {
        var value = new Union<string, int, Dictionary<string, dynamic>>("test");
        var json = JsonSerializer.Serialize(value);
        Assert.Equal("\"test\"", json);

        value = 200;
        json = JsonSerializer.Serialize(value);
        Assert.Equal("200", json);

        value = new Dictionary<string, dynamic>()
        {
            { "hello", "world" },
            { "test", 123 }
        };

        json = JsonSerializer.Serialize(value);
        Assert.Equal("{\"hello\":\"world\",\"test\":123}", json);
    }
}