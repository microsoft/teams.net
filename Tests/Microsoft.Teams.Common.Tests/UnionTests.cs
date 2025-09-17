using System.Text.Json;

namespace Microsoft.Teams.Common.Tests;

public class UnionTests
{
    [Fact]
    public void JsonSerialize()
    {
        var value = new Union<string, int, Dictionary<string, object>>("test");
        var json = JsonSerializer.Serialize(value);
        Assert.Equal("\"test\"", json);

        value = 200;
        json = JsonSerializer.Serialize(value);
        Assert.Equal("200", json);

        value = new Dictionary<string, object>()
        {
            { "hello", "world" },
            { "test", 123 }
        };

        json = JsonSerializer.Serialize(value);
        Assert.Equal("{\"hello\":\"world\",\"test\":123}", json);
    }

    [Fact]
    public void JsonDeserialize()
    {
        // Test deserializing string value
        var stringJson = "\"test\"";
        var stringResult = JsonSerializer.Deserialize<Union<string, int>>(stringJson);
        Assert.Equal("test", stringResult.Value);

        // Test deserializing int value
        var intJson = "200";
        var intResult = JsonSerializer.Deserialize<Union<string, int>>(intJson);
        Assert.Equal(200, intResult.Value);

        // Test deserializing float value (relevant to Column.Width issue)
        var floatJson = "1.5";
        var floatResult = JsonSerializer.Deserialize<Union<string, float>>(floatJson);
        Assert.Equal(1.5f, floatResult.Value);

        // Test deserializing string value for Union<string, float> (the "auto" case)
        var autoJson = "\"auto\"";
        var autoResult = JsonSerializer.Deserialize<Union<string, float>>(autoJson);
        Assert.Equal("auto", autoResult.Value);

        // Test deserializing complex object
        var objectJson = "{\"hello\":\"world\",\"test\":123}";
        var objectResult = JsonSerializer.Deserialize<Union<string, Dictionary<string, object>>>(objectJson);
        Assert.IsType<Dictionary<string, object>>(objectResult.Value);
        var dict = (Dictionary<string, object>)objectResult.Value;
        Assert.Equal("world", dict["hello"].ToString());
        Assert.Equal(123, ((System.Text.Json.JsonElement)dict["test"]).GetInt32());
    }

    [Fact]
    public void IUnion_JsonSerialize()
    {
        // Test that IUnion interface properties serialize correctly (not as {"Value":x})
        IUnion<string, int> iUnionString = new Union<string, int>("test");
        var json = JsonSerializer.Serialize(iUnionString);
        Assert.Equal("\"test\"", json);

        IUnion<string, int> iUnionInt = new Union<string, int>(200);
        json = JsonSerializer.Serialize(iUnionInt);
        Assert.Equal("200", json);

        // Test in object context (like TaskInfo.Width)
        var obj = new { width = (IUnion<int, string>)new Union<int, string>(500) };
        json = JsonSerializer.Serialize(obj);
        Assert.Equal("{\"width\":500}", json);
    }

    [Fact]
    public void IUnion_JsonDeserialize()
    {
        // Test deserializing to IUnion interface
        var stringJson = "\"test\"";
        var stringResult = JsonSerializer.Deserialize<IUnion<string, int>>(stringJson);
        Assert.NotNull(stringResult);
        Assert.Equal("test", stringResult.Value);

        var intJson = "200";
        var intResult = JsonSerializer.Deserialize<IUnion<string, int>>(intJson);
        Assert.NotNull(intResult);
        Assert.Equal(200, intResult.Value);
    }
}
