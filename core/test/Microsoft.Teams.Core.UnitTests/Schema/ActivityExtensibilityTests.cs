// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json.Serialization;

using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.UnitTests.Schema;

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

