using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.Bot.Core.Schema;

public class ExtendedPropertiesDictionary : Dictionary<string, object?> { }

public class CoreActivity() : CoreActivity<ChannelData>()
{
    public static new CoreActivity FromJsonString(string json) => JsonSerializer.Deserialize<CoreActivity>(json, DefaultJsonOptions)!;
    public static new ValueTask<CoreActivity?> FromJsonStreamAsync(Stream stream, CancellationToken cancellationToken = default) =>
        JsonSerializer.DeserializeAsync<CoreActivity>(stream, DefaultJsonOptions, cancellationToken);
}

public class CoreActivity<TChannelData>(string type = "message") where TChannelData : ChannelData, new()
{
    [JsonPropertyName("type")] public string Type { get; set; } = type;
    [JsonPropertyName("channelId")] public string? ChannelId { get; set; }
    [JsonPropertyName("text")] public string? Text { get; set; }
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("serviceUrl")] public string? ServiceUrl { get; set; }
    [JsonPropertyName("replyToId")] public string? ReplyToId { get; set; }
    [JsonPropertyName("channelData")] public TChannelData? ChannelData { get; set; }
    [JsonPropertyName("from")] public ConversationAccount From { get; set; } = new();
    [JsonPropertyName("recipient")] public ConversationAccount Recipient { get; set; } = new();
    [JsonPropertyName("conversation")] public Conversation Conversation { get; set; } = new();
    [JsonPropertyName("entities")] public JsonArray? Entities { get; set; }
    [JsonExtensionData] public ExtendedPropertiesDictionary Properties { get; set; } = [];

    public readonly static JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string ToJson() => JsonSerializer.Serialize(this, DefaultJsonOptions);

    public static CoreActivity<TChannelData> FromJsonString(string json)
        => JsonSerializer.Deserialize<CoreActivity<TChannelData>>(json, DefaultJsonOptions)!;

    public static ValueTask<CoreActivity<TChannelData>?> FromJsonStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        => JsonSerializer.DeserializeAsync<CoreActivity<TChannelData>>(stream, DefaultJsonOptions, cancellationToken);

    public CoreActivity CreateReplyActivity(string text = "")
    {
        CoreActivity result = new()
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
