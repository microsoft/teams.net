// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.Teams.Core.Schema;

/// <summary>
/// Represents an outbound (outgoing) activity that a bot constructs and sends.
/// </summary>
/// <remarks>
/// Unlike <see cref="CoreActivity"/> (which models the full inbound wire shape), an
/// <see cref="CoreActivityInput"/> carries only what a sender is expected to populate: the
/// activity content and body-level identity. Transport routing (service url and conversation id)
/// is supplied explicitly to the API clients at send time, not stamped on the activity.
/// This type is the serialized outbound payload.
/// </remarks>
public class CoreActivityInput
{
    /// <summary>
    /// Gets or sets the type of the activity. See <see cref="ActivityType"/> for common values.
    /// </summary>
    [JsonPropertyName("type")] public string Type { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the activity.
    /// </summary>
    [JsonPropertyName("id")] public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the id of the activity this activity is a reply to. When set, the send is
    /// threaded under the referenced activity.
    /// </summary>
    [JsonPropertyName("replyToId")] public string? ReplyToId { get; set; }

    /// <summary>
    /// Gets or sets the recipient account for this activity. Populated when the send targets a
    /// specific recipient (for example, a targeted message visible only to the inbound sender).
    /// </summary>
    [JsonPropertyName("recipient")] public ChannelAccount? Recipient { get; set; }

    /// <summary>
    /// Gets the extension data dictionary for storing additional properties not defined in the schema.
    /// </summary>
    [JsonExtensionData] public ExtendedPropertiesDictionary Properties { get; set; } = [];

    /// <summary>
    /// Creates a new instance of the <see cref="CoreActivityInput"/> class with the specified activity type.
    /// Defaults to <see cref="ActivityType.Message"/>.
    /// </summary>
    /// <param name="type">The activity type. Defaults to "message".</param>
    [JsonConstructor]
    public CoreActivityInput(string type = ActivityType.Message)
    {
        Type = type;
    }

    /// <summary>
    /// Serializes the current activity to a JSON string.
    /// </summary>
    /// <returns>A JSON string representation of the activity.</returns>
    public virtual string ToJson()
        => JsonSerializer.Serialize(this, CoreActivityJsonContext.Default.CoreActivityInput);

    /// <summary>
    /// Serializes the current activity to a JSON string using the specified <see cref="JsonTypeInfo{T}"/> for source-generated serialization.
    /// </summary>
    /// <typeparam name="T">The type of the activity to serialize. Must inherit from <see cref="CoreActivityInput"/>.</typeparam>
    /// <param name="jsonTypeInfo">The JSON type info that provides serialization metadata for type <typeparamref name="T"/>.</param>
    /// <returns>A JSON string representation of the activity.</returns>
    public string ToJson<T>(JsonTypeInfo<T> jsonTypeInfo) where T : CoreActivityInput
        => JsonSerializer.Serialize(this, jsonTypeInfo);

    /// <summary>
    /// Creates a new instance of the <see cref="CoreActivityInputBuilder"/> to construct outbound activity instances.
    /// </summary>
    /// <returns>A new <see cref="CoreActivityInputBuilder"/> instance.</returns>
    public static CoreActivityInputBuilder CreateBuilder() => new();

    /// <summary>
    /// Deserializes an outbound <see cref="CoreActivityInput"/> from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize. Cannot be null.</param>
    /// <returns>The deserialized <see cref="CoreActivityInput"/>.</returns>
    public static CoreActivityInput FromJsonString(string json)
        => JsonSerializer.Deserialize(json, CoreActivityJsonContext.Default.CoreActivityInput)!;

    /// <summary>
    /// Creates an outbound <see cref="CoreActivityInput"/> from a full (inbound-shaped) <see cref="CoreActivity"/>.
    /// Copies the activity type, id, recipient, and content (extension properties). Transport routing
    /// (conversation, service url) and the sender (from) are supplied explicitly to the API clients and
    /// are not carried through this conversion.
    /// </summary>
    /// <param name="activity">The source activity. Cannot be null.</param>
    /// <returns>A new <see cref="CoreActivityInput"/> carrying the activity's serializable content.</returns>
    public static CoreActivityInput FromActivity(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        CoreActivityInput input = new(activity.Type) { Id = activity.Id, Recipient = activity.Recipient };
        foreach (KeyValuePair<string, object?> kv in activity.Properties)
        {
            input.Properties[kv.Key] = kv.Value;
        }

        return input;
    }
}
