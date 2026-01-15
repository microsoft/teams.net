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
        MyCustomActivity deserializedActivity = MyCustomActivity.FromActivity(CoreActivity.FromJsonString(json));
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
        string json = CoreActivity.ToJson(customChannelDataActivity);
        MyCustomChannelDataActivity deserializedActivity = MyCustomChannelDataActivity.FromActivity(CoreActivity.FromJsonString(json));
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
        MyCustomChannelDataActivity deserializedActivity = MyCustomChannelDataActivity.FromActivity(CoreActivity.FromJsonString(json));
        Assert.NotNull(deserializedActivity);
        Assert.NotNull(deserializedActivity.ChannelData);
        Assert.Equal("customFieldValue", deserializedActivity.ChannelData.CustomField);
        Assert.Equal("12345", deserializedActivity.ChannelData.MyChannelId);
    }
}

public class MyCustomActivity : CoreActivity
{
    internal static MyCustomActivity FromActivity(CoreActivity activity)
    {
        return new MyCustomActivity
        {
            Type = activity.Type,
            ChannelId = activity.ChannelId,
            Id = activity.Id,
            ServiceUrl = activity.ServiceUrl,
            ChannelData = activity.ChannelData,
            From = activity.From,
            Recipient = activity.Recipient,
            Conversation = activity.Conversation,
            Entities = activity.Entities,
            Attachments = activity.Attachments,
            Value = activity.Value,
            Properties = activity.Properties,
            CustomField = activity.Properties.TryGetValue("customField", out object? customFieldObj)
                && customFieldObj is JsonElement jeCustomField
                && jeCustomField.ValueKind == JsonValueKind.String
                    ? jeCustomField.GetString()
                    : null
        };
    }
    [JsonPropertyName("customField")]
    public string? CustomField { get; set; }
}


public class MyChannelData : ChannelData
{
    public MyChannelData()
    {
    }
    public MyChannelData(ChannelData cd)
    {
        if (cd is not null)
        {
            if (cd.Properties.TryGetValue("customField", out object? channelIdObj)
                && channelIdObj is JsonElement jeChannelId
                && jeChannelId.ValueKind == JsonValueKind.String)
            {
                CustomField = jeChannelId.GetString();
            }

            if (cd.Properties.TryGetValue("myChannelId", out object? mychannelIdObj)
                && mychannelIdObj is JsonElement jemyChannelId
                && jemyChannelId.ValueKind == JsonValueKind.String)
            {
                MyChannelId = jemyChannelId.GetString();
            }
        }
    }

    [JsonPropertyName("customField")]
    public string? CustomField { get; set; }

    [JsonPropertyName("myChannelId")]
    public string? MyChannelId { get; set; }
}

public class MyCustomChannelDataActivity : CoreActivity
{
    [JsonPropertyName("channelData")]
    public new MyChannelData? ChannelData { get; set; }

    internal static MyCustomChannelDataActivity FromActivity(CoreActivity coreActivity)
    {
        return new MyCustomChannelDataActivity
        {
            Type = coreActivity.Type,
            ChannelId = coreActivity.ChannelId,
            Id = coreActivity.Id,
            ServiceUrl = coreActivity.ServiceUrl,
            ChannelData = new MyChannelData(coreActivity.ChannelData ?? new Core.Schema.ChannelData()),
            Recipient = coreActivity.Recipient,
            Conversation = coreActivity.Conversation,
            Entities = coreActivity.Entities,
            Attachments = coreActivity.Attachments,
            Value = coreActivity.Value,
            Properties = coreActivity.Properties
        };
    }
}
