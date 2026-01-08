// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json.Serialization;

using Microsoft.Bot.Core.Schema;

namespace Microsoft.Bot.Core.UnitTests.Schema;

public class ActivityExtensibilityTests
{
    [Fact]
    public void CustomActivity_ExtendedProperties_SerializedAndDeserialized()
    {
        MyCustomActivity customActivity = new()
        {
            CustomField = "CustomValue"
        };
        string json = MyCustomActivity.ToJson<MyCustomActivity>(customActivity);
        MyCustomActivity deserializedActivity = CoreActivity.FromJsonString<MyCustomActivity>(json);
        Assert.NotNull(deserializedActivity);
        Assert.Equal("CustomValue", deserializedActivity.CustomField);
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
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
        MyCustomActivity? deserializedActivity = await CoreActivity.FromJsonStreamAsync<MyCustomActivity>(stream);
        Assert.NotNull(deserializedActivity);
        Assert.Equal("CustomValue", deserializedActivity!.CustomField);
    }


    [Fact]
    public void CustomChannelDataActivity_ExtendedProperties_SerializedAndDeserialized()
    {
        MyCustomChannelDataActivity customChannelDataActivity = new()
        {
            ChannelData = new MyChannelData
            {
                CustomField = "customFieldValue",
                MyChannelId = "12345"
            }
        };
        string json = MyCustomChannelDataActivity.ToJson<MyCustomChannelDataActivity>(customChannelDataActivity);
        MyCustomChannelDataActivity deserializedActivity = CoreActivity.FromJsonString<MyCustomChannelDataActivity>(json);
        Assert.NotNull(deserializedActivity);
        Assert.NotNull(deserializedActivity.ChannelData);
        Assert.Equal(ActivityType.Message, deserializedActivity.Type);
        Assert.Equal("customFieldValue", deserializedActivity.ChannelData.CustomField);
        Assert.Equal("12345", deserializedActivity.ChannelData.MyChannelId);
    }


    [Fact]
    public void Deserialize_CustomChannelDataActivity()
    {
        string json = """
        {
            "type": "message",
            "channelData": {
                "customField": "customFieldValue",
                "myChannelId": "12345"
            }
        }
        """;
        MyCustomChannelDataActivity deserializedActivity = CoreActivity.FromJsonString<MyCustomChannelDataActivity>(json);
        Assert.NotNull(deserializedActivity);
        Assert.NotNull(deserializedActivity.ChannelData);
        Assert.Equal("customFieldValue", deserializedActivity.ChannelData.CustomField);
        Assert.Equal("12345", deserializedActivity.ChannelData.MyChannelId);
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

    [JsonPropertyName("myChannelId")]
    public string? MyChannelId { get; set; }
}

public class MyCustomChannelDataActivity : CoreActivity
{
    [JsonPropertyName("channelData")]
    public new MyChannelData? ChannelData { get; set; }
}
