using System.Text.Json.Serialization;

using Microsoft.Teams.Api.Entities;

namespace Microsoft.Teams.Api;

/// <summary>
/// Channel data specific to messages received in Microsoft Teams
/// </summary>
public class ChannelData
{
    /// <summary>
    /// Information about the channel in which the message was sent
    /// </summary>
    [JsonPropertyName("channel")]
    [JsonPropertyOrder(0)]
    public Channel? Channel { get; set; }

    /// <summary>
    /// the event type
    /// </summary>
    [JsonPropertyName("eventType")]
    [JsonPropertyOrder(1)]
    public string? EventType { get; set; }

    /// <summary>
    /// Information about the team in which the message was sent
    /// </summary>
    [JsonPropertyName("team")]
    [JsonPropertyOrder(2)]
    public Team? Team { get; set; }

    /// <summary>
    /// Information about the tenant in which the message was sent
    /// </summary>
    [JsonPropertyName("tenant")]
    [JsonPropertyOrder(3)]
    public Tenant? Tenant { get; set; }

    /// <summary>
    /// Notification settings for the message
    /// </summary>
    [JsonPropertyName("notification")]
    [JsonPropertyOrder(4)]
    public Notification? Notification { get; set; }

    /// <summary>
    /// Information about the settings in which the message was sent
    /// </summary>
    [JsonPropertyName("settings")]
    [JsonPropertyOrder(5)]
    public ChannelDataSettings? Settings { get; set; }

    /// <summary>
    /// Information about the app sending this activity
    /// </summary>
    [JsonPropertyName("app")]
    [JsonPropertyOrder(6)]
    public App? App { get; set; }

    /// <summary>
    /// Whether or not the feedback loop feature is enabled
    /// </summary>
    [JsonPropertyName("feedbackLoopEnabled")]
    [JsonPropertyOrder(7)]
    public bool? FeedbackLoopEnabled { get; set; }

    [JsonPropertyName("streamId")]
    [JsonPropertyOrder(8)]
    public string? StreamId { get; set; }

    [JsonPropertyName("streamType")]
    [JsonPropertyOrder(9)]
    public StreamType? StreamType { get; set; }

    [JsonPropertyName("streamSequence")]
    [JsonPropertyOrder(10)]
    public int? StreamSequence { get; set; }

    /// <summary>
    /// All extra data present
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();

    /// <summary>
    /// merge two channel data objects
    /// </summary>
    /// <param name="from">the object to copy from</param>
    public ChannelData Merge(ChannelData from)
    {
        foreach (var property in GetType().GetProperties().Where(p => p.CanRead && p.CanWrite))
        {
            var value = property.GetValue(from, null);

            if (value != null)
            {
                property.SetValue(this, value, null);
            }
        }

        return this;
    }
}

/// <summary>
/// Settings within teams channel data specific to messages received in Microsoft Teams
/// </summary>
public class ChannelDataSettings
{
    /// <summary>
    /// Information about the selected Teams channel
    /// </summary>
    [JsonPropertyName("selectedChannel")]
    [JsonPropertyOrder(0)]
    public required Channel SelectedChannel { get; set; }

    /// <summary>
    /// @member {any} [any] Additional properties that are not otherwise defined by the TeamsChannelDataSettings
    /// type but that might appear in the REST JSON object.
    /// @remarks With this, properties not represented in the defined type are not dropped when
    /// the JSON object is deserialized, but are instead stored in this property. Such properties
    /// will be written to a JSON object when the instance is serialized.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
}