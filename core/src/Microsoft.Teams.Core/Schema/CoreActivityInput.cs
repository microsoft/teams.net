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
    /// Creates an outbound <see cref="CoreActivityInput"/> from a full (inbound-shaped) <see cref="CoreActivity"/>.
    /// Copies the activity content (extension properties) and body-level identity fields, which are
    /// carried through the outbound extension data. Transport routing (service url, conversation)
    /// is supplied explicitly to the API clients and is not part of the serialized body.
    /// </summary>
    /// <param name="activity">The source activity. Cannot be null.</param>
    /// <returns>A new <see cref="CoreActivityInput"/> carrying the activity's serializable content.</returns>
    public static CoreActivityInput FromActivity(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        CoreActivityInput input = new(activity.Type) { Id = activity.Id };
        foreach (KeyValuePair<string, object?> kv in activity.Properties)
        {
            input.Properties[kv.Key] = kv.Value;
        }

        if (activity.ChannelId is not null) input.Properties["channelId"] = activity.ChannelId;
        if (activity.ReplyToId is not null) input.Properties["replyToId"] = activity.ReplyToId;
        if (activity.From is not null) input.Properties["from"] = activity.From;
        if (activity.Recipient is not null) input.Properties["recipient"] = activity.Recipient;
        if (activity.Conversation is not null) input.Properties["conversation"] = activity.Conversation;
        if (activity.ServiceUrl is not null) input.Properties["serviceUrl"] = activity.ServiceUrl.ToString();

        return input;
    }
}
