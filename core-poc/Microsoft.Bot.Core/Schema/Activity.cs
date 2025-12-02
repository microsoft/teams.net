using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.Bot.Core.Schema;

public class ExtendedPropertiesDictionary : Dictionary<string, object?> { }

public class Activity() : Activity<ChannelData>()
{
    public static new Activity FromJsonString(string json) => JsonSerializer.Deserialize<Activity>(json, DefaultJsonOptions)!;
    public static new ValueTask<Activity?> FromJsonStreamAsync(Stream stream, CancellationToken cancellationToken = default) =>
        JsonSerializer.DeserializeAsync<Activity>(stream, DefaultJsonOptions, cancellationToken);
}

public class Activity<TChannelData>(string type = "message") where TChannelData : ChannelData, new()
{
    [JsonPropertyName("type")] public string Type { get; set; } = type;
    [JsonPropertyName("channelId")] public string? ChannelId { get; set; }
    [JsonPropertyName("text")] public string? Text { get; set; }
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("serviceUrl")] public string? ServiceUrl { get; set; }
    [JsonPropertyName("replyToId")] public string? ReplyToId { get; set; }
    [JsonPropertyName("channelData")] public TChannelData? ChannelData { get; set; }
    [JsonPropertyName("from")] public ConversationAccount? From { get; set; }
    [JsonPropertyName("recipient")] public ConversationAccount? Recipient { get; set; }
    [JsonPropertyName("conversation")] public Conversation? Conversation { get; set; }
    [JsonPropertyName("entities")] public JsonArray? Entities { get; set; }
    [JsonExtensionData] public ExtendedPropertiesDictionary Properties { get; set; } = [];

    public readonly static JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string ToJson() => JsonSerializer.Serialize(this, DefaultJsonOptions);

    public static Activity<TChannelData> FromJsonString(string json)
        => JsonSerializer.Deserialize<Activity<TChannelData>>(json, DefaultJsonOptions)!;

    public static ValueTask<Activity<TChannelData>?> FromJsonStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        => JsonSerializer.DeserializeAsync<Activity<TChannelData>>(stream, DefaultJsonOptions, cancellationToken);

    public Activity CreateReplyActivity(string text = "")
    {
        Activity result = new()
        {
            Type = "message",
            ChannelId = ChannelId,
            ServiceUrl = ServiceUrl,
            Conversation = Conversation,
            From = Recipient,
            Recipient = From,
            ReplyToId = Id,
            Text = text
        };
        return result!;
    }
}
