using System.Text;
using System.Text.Json.Serialization;

using Microsoft.Bot.Core.Schema;

namespace Microsoft.Bot.Core.UnitTests.Schema;

public class ActivityExtensibilityTests
{
    [Fact]
    public void CustomActivity_ExtendedProperties_SerializedAndDeserialized()
    {
        var customActivity = new MyCustomActivity
        {
            CustomField = "CustomValue"
        };
        string json = MyCustomActivity.ToJson<MyCustomActivity>(customActivity);
        var deserializedActivity = CoreActivity.FromJsonString<MyCustomActivity>(json);
        Assert.NotNull(deserializedActivity);
        Assert.Equal("CustomValue", deserializedActivity!.CustomField);
    }

    [Fact]
    public async Task CustomActivity_ExtendedProperties_SerializedAndDeserialized_Async()
    {
        string json = """
        {
            "type": "message",
            "customField": "CustomValue"
        }
        """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var deserializedActivity = await CoreActivity.FromJsonStreamAsync<MyCustomActivity>(stream);
        Assert.NotNull(deserializedActivity);
        Assert.Equal("CustomValue", deserializedActivity!.CustomField);
    }


    [Fact]
    public void CustomChannelDataActivity_ExtendedProperties_SerializedAndDeserialized()
    {
        var customChannelDataActivity = new MyCustomChannelDataActivity
        {
            ChannelData = new MyChannelData
            {
                CustomField = "ChannelDataValue"
            }
        };
        var json = MyCustomChannelDataActivity.ToJson<MyCustomChannelDataActivity>(customChannelDataActivity);
        var deserializedActivity = CoreActivity.FromJsonString<MyCustomChannelDataActivity>(json);
        Assert.NotNull(deserializedActivity);
        Assert.NotNull(deserializedActivity!.ChannelData);
        Assert.Equal("ChannelDataValue", deserializedActivity.ChannelData!.CustomField);
    }
}

public class MyCustomActivity : CoreActivity
{
    [JsonPropertyName("customField")]
    public string? CustomField { get; set; }
}


public class MyChannelData : ChannelData
{
    [JsonPropertyName("customField")]
    public string? CustomField { get; set; }
}

public class MyCustomChannelDataActivity : CoreActivity
{
    [JsonPropertyName("customField")]
    public new MyChannelData? ChannelData { get; set; }
}