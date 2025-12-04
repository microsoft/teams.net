using System.Text.Json;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Tests;


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

    [Fact]
    public void CompareShouldWork()
    {
        var value1 = new StringEnum("test");
        var value2 = new StringEnum("test");
        var value3 = new StringEnum("different");
        
        Assert.True(value1.Equals(value2));
        Assert.False(value1.Equals(value3));

        var c1 = new ChannelId("test");
        var c2 = new ChannelId("test");
        Assert.Equal(c1, c2);
    }
}

