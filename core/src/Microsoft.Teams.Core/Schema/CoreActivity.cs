// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.Teams.Core.Schema;

/// <summary>
/// Represents a dictionary for storing extended properties as key-value pairs.
/// </summary>
public class ExtendedPropertiesDictionary : Dictionary<string, object?>
{
    /// <summary>
    /// Initializes a new empty instance of the <see cref="ExtendedPropertiesDictionary"/> class.
    /// </summary>
    public ExtendedPropertiesDictionary() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtendedPropertiesDictionary"/> class by shallow-copying entries from another dictionary.
    /// </summary>
    public ExtendedPropertiesDictionary(IDictionary<string, object?> source) : base(source) { }

    /// <summary>
    /// Extracts and deserializes a value from the dictionary, removing the entry if found.
    /// Returns the deserialized value, or default if the key is not present.
    /// </summary>
    public T? Extract<T>(string key)
    {
        if (!TryGetValue(key, out object? raw))
            return default;

        Remove(key);

        if (raw is T typed)
            return typed;

        if (raw is string rawString)
        {
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(rawString));
        }

        if (raw is System.Text.Json.JsonElement element)
            return System.Text.Json.JsonSerializer.Deserialize<T>(element.GetRawText());

        return default;
    }

    /// <summary>
    /// Gets and deserializes a value from the dictionary without removing it.
    /// Handles <see cref="System.Text.Json.JsonElement"/> values that result from deserialization.
    /// </summary>
    public T? Get<T>(string key)
    {
        if (!TryGetValue(key, out object? raw))
            return default;

        if (raw is T typed)
            return typed;

        if (raw is string rawString)
        {
            T? deserialized = System.Text.Json.JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(rawString));
            this[key] = deserialized;
            return deserialized;
        }

        if (raw is System.Text.Json.JsonElement element)
        {
            T? deserialized = System.Text.Json.JsonSerializer.Deserialize<T>(element.GetRawText());
            this[key] = deserialized;
            return deserialized;
        }

        return default;
    }
}

/// <summary>
/// Represents a core activity object that encapsulates the data and metadata for a bot interaction.
/// </summary>
/// <remarks>
/// This class provides the foundational structure for bot activities including message exchanges,
/// conversation updates, and other bot-related events. It supports serialization to and from JSON
/// and includes extension properties for channel-specific data.
/// Follows the Activity Protocol Specification: https://github.com/microsoft/Agents/blob/main/specs/activity/protocol-activity.md
/// </remarks>
public class CoreActivity
{
    /// <summary>
    /// Gets or sets the type of the activity. See <see cref="ActivityType"/> for common values.
    /// </summary>
    /// <remarks>
    /// Common activity types include "message", "conversationUpdate", "contactRelationUpdate", etc.
    /// </remarks>
    [JsonPropertyName("type")] public string Type { get; set; }
    /// <summary>
    /// Gets or sets the unique identifier for the channel on which this activity is occurring.
    /// </summary>
    [JsonPropertyName("channelId")] public string? ChannelId { get; set; }
    /// <summary>
    /// Gets or sets the unique identifier for the activity.
    /// </summary>
    [JsonPropertyName("id")] public string? Id { get; set; }
    /// <summary>
    /// Gets or sets the URL of the service endpoint for this activity.
    /// </summary>
    /// <remarks>
    /// This URL is used to send responses back to the channel.
    /// </remarks>
    [JsonPropertyName("serviceUrl")] public Uri? ServiceUrl { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the activity this activity is a reply to.
    /// </summary>
    [JsonPropertyName("replyToId")] public string? ReplyToId { get; set; }

    /// <summary>
    /// Gets or sets the conversation information for this activity.
    /// </summary>
    [JsonPropertyName("conversation")] public Conversation? Conversation { get; set; }

    /// <summary>
    /// Gets or sets the sender account for this activity.
    /// </summary>
    [JsonPropertyName("from")] public ChannelAccount? From { get; set; }

    /// <summary>
    /// Gets or sets the recipient account for this activity.
    /// </summary>
    [JsonPropertyName("recipient")] public ChannelAccount? Recipient { get; set; }

    /// <summary>
    /// Gets the extension data dictionary for storing additional properties not defined in the schema.
    /// </summary>
    [JsonExtensionData] public ExtendedPropertiesDictionary Properties { get; set; } = [];

    /// <summary>
    /// Gets the JSON serializer options used for reflection-based serialization of extended activity types.
    /// </summary>
    /// <remarks>
    /// Uses reflection-based serialization to support custom activity types that extend CoreActivity.
    /// This is used when serializing/deserializing types not registered in the source-generated context,
    /// and to serialize inbound (read-model) activities by their runtime type for logging/diagnostics.
    /// Inbound activities are never serialized onto the wire; the outbound <c>*Input</c> types own the
    /// AOT-safe serialization path.
    /// </remarks>
    public static readonly JsonSerializerOptions ReflectionJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates a new instance of the <see cref="CoreActivity"/> class with the specified activity type.
    /// Defaults to <see cref="ActivityType.Message"/>.
    /// </summary>
    /// <param name="type">The activity type. Defaults to "message".</param>
    [JsonConstructor]
    public CoreActivity(string type = ActivityType.Message)
    {
        Type = type;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="CoreActivity"/> class by copying properties from another activity.
    /// </summary>
    /// <param name="activity">The source activity to copy from.</param>
    protected CoreActivity(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        Id = activity.Id;
        ServiceUrl = activity.ServiceUrl;
        ChannelId = activity.ChannelId;
        Type = activity.Type;
        Conversation = activity.Conversation is not null ? new Conversation(activity.Conversation.Id) { Properties = new ExtendedPropertiesDictionary(activity.Conversation.Properties) } : null;
        From = activity.From is not null ? CloneChannelAccount(activity.From) : null;
        Recipient = activity.Recipient is not null ? CloneChannelAccount(activity.Recipient) : null;
        Properties = new ExtendedPropertiesDictionary(activity.Properties);
    }

#pragma warning disable ExperimentalTeamsTargeted
    private static ChannelAccount CloneChannelAccount(ChannelAccount source) => new()
    {
        Id = source.Id,
        BotId = source.BotId,
        Name = source.Name,
        IsTargeted = source.IsTargeted,
        AgenticAppId = source.AgenticAppId,
        AgenticUserId = source.AgenticUserId,
        AgenticAppBlueprintId = source.AgenticAppBlueprintId,
        TenantId = source.TenantId,
        Properties = new ExtendedPropertiesDictionary(source.Properties)
    };
#pragma warning restore ExperimentalTeamsTargeted

    /// <summary>
    /// Serializes the current activity to a JSON string.
    /// </summary>
    /// <returns>A JSON string representation of the activity.</returns>
    public virtual string ToJson()
        => JsonSerializer.Serialize(this, CoreActivityJsonContext.Default.CoreActivity);

    /// <summary>
    /// Serializes the current activity to a JSON string using the specified <see cref="JsonTypeInfo{T}"/> for source-generated serialization.
    /// </summary>
    /// <typeparam name="T">The type of the activity to serialize. Must inherit from <see cref="CoreActivity"/>.</typeparam>
    /// <param name="jsonTypeInfo">The JSON type info that provides serialization metadata for type <typeparamref name="T"/>.</param>
    /// <returns>A JSON string representation of the activity.</returns>
    public string ToJson<T>(JsonTypeInfo<T> jsonTypeInfo) where T : CoreActivity
        => JsonSerializer.Serialize(this, jsonTypeInfo);

    /// <summary>
    /// Serializes the specified activity instance to a JSON string using reflection-based serialization.
    /// </summary>
    /// <remarks>Uses reflection-based serialization to support custom activity types that extend
    /// <see cref="CoreActivity"/>. The resulting JSON reflects the public properties of the activity instance.</remarks>
    /// <typeparam name="T">The type of the activity to serialize. Must inherit from CoreActivity.</typeparam>
    /// <param name="instance">The activity instance to serialize. Cannot be null.</param>
    /// <returns>A JSON string representation of the specified activity instance.</returns>
    public static string ToJson<T>(T instance) where T : CoreActivity
        => JsonSerializer.Serialize<T>(instance, ReflectionJsonOptions);

    /// <summary>
    /// Deserializes a JSON string into a <see cref="CoreActivity"/> object.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A <see cref="CoreActivity"/> instance.</returns>
    public static CoreActivity FromJsonString(string json)
        => JsonSerializer.Deserialize(json, CoreActivityJsonContext.Default.CoreActivity)!;

    /// <summary>
    /// Asynchronously deserializes a JSON stream into a <see cref="CoreActivity"/> object.
    /// </summary>
    /// <param name="stream">The stream containing JSON data to deserialize.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized <see cref="CoreActivity"/> instance, or null if deserialization fails.</returns>
    public static ValueTask<CoreActivity?> FromJsonStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        => JsonSerializer.DeserializeAsync(stream, CoreActivityJsonContext.Default.CoreActivity, cancellationToken);

    /// <summary>
    /// Asynchronously deserializes a JSON stream into an instance of type <typeparamref name="T"/> using the specified <see cref="JsonTypeInfo{T}"/> for source-generated serialization.
    /// </summary>
    /// <typeparam name="T">The type of the activity to deserialize. Must inherit from <see cref="CoreActivity"/>.</typeparam>
    /// <param name="stream">The stream containing JSON data to deserialize.</param>
    /// <param name="jsonTypeInfo">The JSON type info that provides deserialization metadata for type <typeparamref name="T"/>.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{T}"/> representing the asynchronous operation. The result contains the deserialized activity, or null if deserialization fails.</returns>
    public static ValueTask<T?> FromJsonStreamAsync<T>(Stream stream, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken = default) where T : CoreActivity
        => JsonSerializer.DeserializeAsync(stream, jsonTypeInfo, cancellationToken);

    /// <summary>
    /// Asynchronously deserializes a JSON value from the specified stream into an instance of type T.
    /// </summary>
    /// <remarks>The caller is responsible for managing the lifetime of the provided stream. The method uses
    /// default JSON serialization options.</remarks>
    /// <typeparam name="T">The type of the object to deserialize. Must derive from CoreActivity.</typeparam>
    /// <param name="stream">The stream containing the JSON data to deserialize. The stream must be readable and positioned at the start of
    /// the JSON content.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A ValueTask that represents the asynchronous operation. The result contains an instance of type T if
    /// deserialization is successful; otherwise, null.</returns>
    public static ValueTask<T?> FromJsonStreamAsync<T>(Stream stream, CancellationToken cancellationToken = default) where T : CoreActivity
        => JsonSerializer.DeserializeAsync<T>(stream, ReflectionJsonOptions, cancellationToken);


}
