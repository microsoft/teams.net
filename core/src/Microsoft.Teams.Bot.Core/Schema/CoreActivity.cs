// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.Teams.Bot.Core.Schema;

/// <summary>
/// Represents a dictionary for storing extended properties as key-value pairs.
/// </summary>
public class ExtendedPropertiesDictionary : Dictionary<string, object?> { }

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
    /// Gets or sets channel-specific data associated with this activity.
    /// </summary>
    [JsonPropertyName("channelData")] public ChannelData? ChannelData { get; set; }
    /// <summary>
    /// Gets or sets the account that sent this activity.
    /// </summary>
    [JsonPropertyName("from")] public ConversationAccount From { get; set; } = new();
    /// <summary>
    /// Gets or sets the account that should receive this activity.
    /// </summary>
    [JsonPropertyName("recipient")] public ConversationAccount Recipient { get; set; } = new();

    /// <summary>
    /// Indicates if this is a targeted message visible only to a specific recipient.
    /// Used internally by the SDK for routing - not serialized to the service.
    /// </summary>
    [JsonIgnore] public bool IsTargeted { get; set; }
    /// <summary>
    /// Gets or sets the conversation in which this activity is taking place.
    /// </summary>
    [JsonPropertyName("conversation")] public Conversation Conversation { get; set; } = new();

    /// <summary>
    /// Gets the collection of entities contained in this activity.
    /// </summary>
    /// <remarks>
    /// Entities are structured objects that represent mentions, places, or other data.
    /// </remarks>
#pragma warning disable CA2227 // Collection properties should be read only
    [JsonPropertyName("entities")] public JsonArray? Entities { get; set; }

    /// <summary>
    /// Gets the collection of attachments associated with this activity.
    /// </summary>
    [JsonPropertyName("attachments")] public JsonArray? Attachments { get; set; }

    // TODO: Can value need be a JSONObject?
    /// <summary>
    /// Gets or sets the value payload of the activity.
    /// </summary>
    [JsonPropertyName("value")] public JsonNode? Value { get; set; }

    /// <summary>
    /// Gets the extension data dictionary for storing additional properties not defined in the schema.
    /// </summary>
    [JsonExtensionData] public ExtendedPropertiesDictionary Properties { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only

    /// <summary>
    /// Gets the default JSON serializer options used for serializing and deserializing activities.
    /// </summary>
    /// <remarks>
    /// Uses the source-generated JSON context for AOT-compatible serialization.
    /// </remarks>
    public static readonly JsonSerializerOptions DefaultJsonOptions = CoreActivityJsonContext.Default.Options;

    /// <summary>
    /// Gets the JSON serializer options used for reflection-based serialization of extended activity types.
    /// </summary>
    /// <remarks>
    /// Uses reflection-based serialization to support custom activity types that extend CoreActivity.
    /// This is used when serializing/deserializing types not registered in the source-generated context.
    /// </remarks>
    private static readonly JsonSerializerOptions ReflectionJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates a new instance of the <see cref="CoreActivity"/> class with the specified activity type.
    /// </summary>
    /// <param name="type"></param>
    public CoreActivity(string type = ActivityType.Message)
    {
        Type = type;
    }


    /// <summary>
    ///  Creates a new instance of the <see cref="CoreActivity"/> class. As Message type by default.
    /// </summary>
    public CoreActivity()
    {
        Type = ActivityType.Message;
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
        // TODO: Figure out why this is needed...
        // ReplyToId = activity.ReplyToId;
        ChannelData = activity.ChannelData;
        From = activity.From;
        Recipient = activity.Recipient;
        Conversation = activity.Conversation;
        Entities = activity.Entities;
        Attachments = activity.Attachments;
        Properties = activity.Properties;
        Value = activity.Value;
        IsTargeted = activity.IsTargeted;
    }

    /// <summary>
    /// Serializes the current activity to a JSON string.
    /// </summary>
    /// <returns>A JSON string representation of the activity.</returns>
    public virtual string ToJson()
        => JsonSerializer.Serialize(this, CoreActivityJsonContext.Default.CoreActivity);

    /// <summary>
    /// Serializes the current activity to a JSON string using the specified JsonTypeInfo options.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ops"></param>
    /// <returns></returns>
    public string ToJson<T>(JsonTypeInfo<T> ops) where T : CoreActivity
        => JsonSerializer.Serialize(this, ops);

    /// <summary>
    /// Serializes the specified activity instance to a JSON string using the default serialization options.
    /// </summary>
    /// <remarks>The serialization uses the default JSON options defined by DefaultJsonOptions. The resulting
    /// JSON reflects the public properties of the activity instance.</remarks>
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
    /// Deserializes a JSON stream into an instance of type T using the specified JsonTypeInfo options.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="stream"></param>
    /// <param name="ops"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static ValueTask<T?> FromJsonStreamAsync<T>(Stream stream, JsonTypeInfo<T> ops, CancellationToken cancellationToken = default) where T : CoreActivity
        => JsonSerializer.DeserializeAsync(stream, ops, cancellationToken);

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

    /// <summary>
    /// Creates a new instance of the <see cref="CoreActivityBuilder"/> to construct activity instances.
    /// </summary>
    /// <returns></returns>
    public static CoreActivityBuilder CreateBuilder() => new();

}
