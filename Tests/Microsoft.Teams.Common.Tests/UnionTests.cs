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
        // Test string deserialization
        var json = "\"test\"";
        var value = JsonSerializer.Deserialize<Union<string, int, Dictionary<string, object>>>(json);
        Assert.Equal("test", value.Value);

        // Test int deserialization
        json = "200";
        value = JsonSerializer.Deserialize<Union<string, int, Dictionary<string, object>>>(json);
        Assert.Equal(200, value.Value);

        // Test dictionary deserialization
        json = "{\"hello\":\"world\",\"test\":123}";
        value = JsonSerializer.Deserialize<Union<string, int, Dictionary<string, object>>>(json);
        Assert.IsType<Dictionary<string, object>>(value.Value);
        var dict = (Dictionary<string, object>)value.Value;
        Assert.Equal("world", dict["hello"]);
        Assert.Equal(123, dict["test"]);
    }

    [Fact]
    public void ToString_Union2()
    {
        // Test string value toString
        var stringValue = new Union<string, int>("test");
        Assert.Equal("test", stringValue.ToString());

        // Test int value toString
        var intValue = new Union<string, int>(42);
        Assert.Equal("42", intValue.ToString());

        // Test custom object toString
        var customObject = new TestObject { Name = "Custom" };
        var objectValue = new Union<TestObject, int>(customObject);
        Assert.Equal("TestObject: Custom", objectValue.ToString());
    }

    [Fact]
    public void ToString_Union3()
    {
        // Test string value toString
        var stringValue = new Union<string, int, bool>("hello");
        Assert.Equal("hello", stringValue.ToString());

        // Test int value toString
        var intValue = new Union<string, int, bool>(123);
        Assert.Equal("123", intValue.ToString());

        // Test bool value toString
        var boolValue = new Union<string, int, bool>(true);
        Assert.Equal("True", boolValue.ToString());
    }

    [Fact]
    public void ToString_Union4()
    {
        // Test string value toString
        var stringValue = new Union<string, int, bool, double>("world");
        Assert.Equal("world", stringValue.ToString());

        // Test int value toString
        var intValue = new Union<string, int, bool, double>(456);
        Assert.Equal("456", intValue.ToString());

        // Test bool value toString
        var boolValue = new Union<string, int, bool, double>(false);
        Assert.Equal("False", boolValue.ToString());

        // Test double value toString
        var doubleValue = new Union<string, int, bool, double>(3.14);
        Assert.Equal("3.14", doubleValue.ToString());
    }

    [Fact]
    public void Equality_Union2()
    {
        var value1 = new Union<string, int>("test");
        var value2 = new Union<string, int>("test");
        var value3 = new Union<string, int>("different");
        var value4 = new Union<string, int>(42);

        Assert.True(value1.Equals(value2));
        Assert.True(value1 == value2);
        Assert.False(value1.Equals(value3));
        Assert.False(value1 == value3);
        Assert.False(value1.Equals(value4));
        Assert.False(value1 == value4);
        Assert.True(value1 != value3);
        Assert.True(value1 != value4);
    }

    [Fact]
    public void Equality_Union3()
    {
        var value1 = new Union<string, int, bool>("test");
        var value2 = new Union<string, int, bool>("test");
        var value3 = new Union<string, int, bool>(42);
        var value4 = new Union<string, int, bool>(true);

        Assert.True(value1.Equals(value2));
        Assert.True(value1 == value2);
        Assert.False(value1.Equals(value3));
        Assert.False(value1 == value3);
        Assert.False(value1.Equals(value4));
        Assert.False(value1 == value4);
    }

    [Fact]
    public void Equality_Union4()
    {
        var value1 = new Union<string, int, bool, double>("test");
        var value2 = new Union<string, int, bool, double>("test");
        var value3 = new Union<string, int, bool, double>(42);

        Assert.True(value1.Equals(value2));
        Assert.True(value1 == value2);
        Assert.False(value1.Equals(value3));
        Assert.False(value1 == value3);
    }

    [Fact]
    public void GetHashCode_Union2()
    {
        var value1 = new Union<string, int>("test");
        var value2 = new Union<string, int>("test");
        var value3 = new Union<string, int>("different");

        Assert.Equal(value1.GetHashCode(), value2.GetHashCode());
        Assert.NotEqual(value1.GetHashCode(), value3.GetHashCode());
    }

    [Fact]
    public void GetHashCode_Union3()
    {
        var value1 = new Union<string, int, bool>("test");
        var value2 = new Union<string, int, bool>("test");
        var value3 = new Union<string, int, bool>(42);

        Assert.Equal(value1.GetHashCode(), value2.GetHashCode());
        Assert.NotEqual(value1.GetHashCode(), value3.GetHashCode());
    }

    [Fact]
    public void GetHashCode_Union4()
    {
        var value1 = new Union<string, int, bool, double>("test");
        var value2 = new Union<string, int, bool, double>("test");
        var value3 = new Union<string, int, bool, double>(42);

        Assert.Equal(value1.GetHashCode(), value2.GetHashCode());
        Assert.NotEqual(value1.GetHashCode(), value3.GetHashCode());
    }

    [Fact]
    public void ImplicitConversion_Union2()
    {
        Union<string, int> stringValue = "test";
        Assert.Equal("test", stringValue.Value);

        Union<string, int> intValue = 42;
        Assert.Equal(42, intValue.Value);
    }

    [Fact]
    public void ImplicitConversion_Union3()
    {
        Union<string, int, bool> stringValue = "test";
        Assert.Equal("test", stringValue.Value);

        Union<string, int, bool> intValue = 42;
        Assert.Equal(42, intValue.Value);

        Union<string, int, bool> boolValue = true;
        Assert.Equal(true, boolValue.Value);
    }

    [Fact]
    public void ImplicitConversion_Union4()
    {
        Union<string, int, bool, double> stringValue = "test";
        Assert.Equal("test", stringValue.Value);

        Union<string, int, bool, double> intValue = 42;
        Assert.Equal(42, intValue.Value);

        Union<string, int, bool, double> boolValue = true;
        Assert.Equal(true, boolValue.Value);

        Union<string, int, bool, double> doubleValue = 3.14;
        Assert.Equal(3.14, doubleValue.Value);
    }

    [Fact]
    public void JsonSerializeDeserialize_Union2()
    {
        var originalString = new Union<string, int>("serialize_test");
        var json = JsonSerializer.Serialize(originalString);
        var deserializedString = JsonSerializer.Deserialize<Union<string, int>>(json);
        Assert.Equal(originalString.Value, deserializedString.Value);

        var originalInt = new Union<string, int>(999);
        json = JsonSerializer.Serialize(originalInt);
        var deserializedInt = JsonSerializer.Deserialize<Union<string, int>>(json);
        Assert.Equal(originalInt.Value, deserializedInt.Value);
    }

    [Fact]
    public void JsonSerializeDeserialize_Union4()
    {
        var originalString = new Union<string, int, bool, double>("serialize_test");
        var json = JsonSerializer.Serialize(originalString);
        var deserializedString = JsonSerializer.Deserialize<Union<string, int, bool, double>>(json);
        Assert.Equal(originalString.Value, deserializedString.Value);

        var originalDouble = new Union<string, int, bool, double>(2.718);
        json = JsonSerializer.Serialize(originalDouble);
        var deserializedDouble = JsonSerializer.Deserialize<Union<string, int, bool, double>>(json);
        Assert.Equal(originalDouble.Value, deserializedDouble.Value);
    }

    [Fact]
    public void Match_Union2()
    {
        var stringValue = new Union<string, int>("match_test");
        var result = stringValue.Match(
            s => $"String: {s}",
            i => $"Int: {i}"
        );
        Assert.Equal("String: match_test", result);

        var intValue = new Union<string, int>(777);
        result = intValue.Match(
            s => $"String: {s}",
            i => $"Int: {i}"
        );
        Assert.Equal("Int: 777", result);
    }

    [Fact]
    public void Switch_Union2()
    {
        var stringValue = new Union<string, int>("switch_test");
        string? capturedString = null;
        int? capturedInt = null;

        stringValue.Switch(
            s => capturedString = s,
            i => capturedInt = i
        );

        Assert.Equal("switch_test", capturedString);
        Assert.Null(capturedInt);

        var intValue = new Union<string, int>(888);
        capturedString = null;
        capturedInt = null;

        intValue.Switch(
            s => capturedString = s,
            i => capturedInt = i
        );

        Assert.Null(capturedString);
        Assert.Equal(888, capturedInt);
    }

    [Fact]
    public void JsonException_OnNullValue()
    {
        var json = "null";
        Assert.Throws<JsonException>(() => 
            JsonSerializer.Deserialize<Union<string, int>>(json));
    }

    [Fact]
    public void ToString_WithNullableReference()
    {
        var nullableString = (string?)null;
        // Union requires notnull, so this should work with a valid fallback
        var stringValue = new Union<string, int>("fallback");
        Assert.Equal("fallback", stringValue.ToString());
    }

    [Fact]
    public void JsonSerialize_ComplexTypes()
    {
        var testObj = new TestObject { Name = "SerializeTest" };
        var unionValue = new Union<TestObject, int>(testObj);
        
        var json = JsonSerializer.Serialize(unionValue);
        var deserializedUnion = JsonSerializer.Deserialize<Union<TestObject, int>>(json);
        
        Assert.IsType<TestObject>(deserializedUnion.Value);
        var deserializedObj = (TestObject)deserializedUnion.Value;
        Assert.Equal("SerializeTest", deserializedObj.Name);
    }

    [Fact]
    public void UnionConverterFactory_CanConvert()
    {
        var factory = new UnionJsonConverterFactory();
        
        // Test it can convert Union types
        Assert.True(factory.CanConvert(typeof(Union<string, int>)));
        Assert.True(factory.CanConvert(typeof(Union<string, int, bool>)));
        Assert.True(factory.CanConvert(typeof(Union<string, int, bool, double>)));
        
        // Test it cannot convert non-Union types
        Assert.False(factory.CanConvert(typeof(string)));
        Assert.False(factory.CanConvert(typeof(int)));
        Assert.False(factory.CanConvert(typeof(List<string>)));
    }

    [Fact]
    public void Match_Union3()
    {
        var stringValue = new Union<string, int, bool>("match3_test");
        var result = stringValue.Match(
            s => $"String: {s}",
            i => $"Int: {i}",
            b => $"Bool: {b}"
        );
        Assert.Equal("String: match3_test", result);

        var boolValue = new Union<string, int, bool>(true);
        result = boolValue.Match(
            s => $"String: {s}",
            i => $"Int: {i}",
            b => $"Bool: {b}"
        );
        Assert.Equal("Bool: True", result);
    }

    [Fact]
    public void Match_Union4()
    {
        var doubleValue = new Union<string, int, bool, double>(2.718);
        var result = doubleValue.Match(
            s => $"String: {s}",
            i => $"Int: {i}",
            b => $"Bool: {b}",
            d => $"Double: {d}"
        );
        Assert.Equal("Double: 2.718", result);
    }

    [Fact]
    public void Switch_Union3()
    {
        var boolValue = new Union<string, int, bool>(false);
        string? capturedString = null;
        int? capturedInt = null;
        bool? capturedBool = null;

        boolValue.Switch(
            s => capturedString = s,
            i => capturedInt = i,
            b => capturedBool = b
        );

        Assert.Null(capturedString);
        Assert.Null(capturedInt);
        Assert.Equal(false, capturedBool);
    }

    [Fact]
    public void Switch_Union4()
    {
        var doubleValue = new Union<string, int, bool, double>(1.414);
        string? capturedString = null;
        int? capturedInt = null;
        bool? capturedBool = null;
        double? capturedDouble = null;

        doubleValue.Switch(
            s => capturedString = s,
            i => capturedInt = i,
            b => capturedBool = b,
            d => capturedDouble = d
        );

        Assert.Null(capturedString);
        Assert.Null(capturedInt);
        Assert.Null(capturedBool);
        Assert.Equal(1.414, capturedDouble);
    }

    private class TestObject
    {
        public string Name { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"TestObject: {Name}";
        }
    }
}