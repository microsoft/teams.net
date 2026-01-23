// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Teams.Bot.Apps.Schema.Entities;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Teams Activity schema.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227: Collection Properties should be read only", Justification = "<Pending>")]
public class TeamsActivity : CoreActivity
{
    /// <summary>
    /// Creates a new instance of the TeamsActivity class from the specified Activity object.
    /// </summary>
    /// <param name="activity">The Activity instance to convert. Cannot be null.</param>
    /// <returns>A TeamsActivity object that represents the specified Activity.</returns>
    public static TeamsActivity FromActivity(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        return TeamsActivityType.ActivityDeserializerMap.TryGetValue(activity.Type, out var factory)
            ? factory.FromActivity(activity)
            : new TeamsActivity(activity);  // Fallback to base type
    }

    /// <summary>
    /// Creates a new instance of the TeamsActivity class from the specified Activity object.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static new TeamsActivity FromJsonString(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        string? type = doc.RootElement.TryGetProperty("type", out JsonElement typeElement)
            ? typeElement.GetString()
            : null;

        return type != null && TeamsActivityType.ActivityDeserializerMap.TryGetValue(type, out var factory)
            ? factory.FromJson(json)
            : FromJsonString(json, TeamsActivityJsonContext.Default.TeamsActivity);
    }

    /// <summary>
    /// Creates a new instance of the specified activity type from JSON string.
    /// </summary>
    /// <typeparam name="T">The expected activity type.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="typeInfo">The JSON type info for deserialization.</param>
    /// <returns>An activity of type T.</returns>
    public static T FromJsonString<T>(string json, JsonTypeInfo<T> typeInfo) where T : TeamsActivity
    {
        T activity = JsonSerializer.Deserialize(json, typeInfo)!;
        activity.Rebase();
        return activity;
    }


    /// <summary>
    /// Overrides the ToJson method to serialize the TeamsActivity object to a JSON string.
    /// </summary>
    /// <returns></returns>
    public new string ToJson()
        => ToJson(TeamsActivityJsonContext.Default.TeamsActivity);

    /// <summary>
    /// Constructor with type parameter.
    /// </summary>
    /// <param name="type"></param>
    public TeamsActivity(string type)
    {
        Type = type;
        From = new TeamsConversationAccount();
        Recipient = new TeamsConversationAccount();
        Conversation = new TeamsConversation();
    }

    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    public TeamsActivity()
    {
        From = new TeamsConversationAccount();
        Recipient = new TeamsConversationAccount();
        Conversation = new TeamsConversation();
    }

    /// <summary>
    /// Protected constructor to create TeamsActivity from CoreActivity.
    /// Allows derived classes to call via base(activity).
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    protected TeamsActivity(CoreActivity activity) : base(activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        // Convert base types to Teams-specific types
        if (activity.ChannelData is not null)
        {
            ChannelData = new TeamsChannelData(activity.ChannelData);
        }
        From = new TeamsConversationAccount(activity.From);
        Recipient = new TeamsConversationAccount(activity.Recipient);
        Conversation = new TeamsConversation(activity.Conversation);
        Attachments = TeamsAttachment.FromJArray(activity.Attachments);
        Entities = EntityList.FromJsonArray(activity.Entities);

        Rebase();
    }

    /// <summary>
    /// Resets shadow properties in base class
    /// </summary>
    /// <returns></returns>
    internal TeamsActivity Rebase()
    {
        base.Attachments = this.Attachments?.ToJsonArray();
        base.Entities = this.Entities?.ToJsonArray();

        return this;
    }


    /// <summary>
    /// Gets or sets the account information for the sender of the Teams conversation.
    /// </summary>
    [JsonPropertyName("from")]
    public new TeamsConversationAccount From
    {
        get => (base.From as TeamsConversationAccount) ?? new TeamsConversationAccount(base.From);
        set => base.From = value;
    }

    /// <summary>
    /// Gets or sets the account information for the recipient of the Teams conversation.
    /// </summary>
    [JsonPropertyName("recipient")]
    public new TeamsConversationAccount Recipient
    {
        get => (base.Recipient as TeamsConversationAccount) ?? new TeamsConversationAccount(base.Recipient);
        set => base.Recipient = value;
    }

    /// <summary>
    /// Gets or sets the conversation information for the Teams conversation.
    /// </summary>
    [JsonPropertyName("conversation")]
    public new TeamsConversation Conversation
    {
        get => (base.Conversation as TeamsConversation) ?? new TeamsConversation(base.Conversation);
        set => base.Conversation = value;
    }

    /// <summary>
    /// Gets or sets the Teams-specific channel data associated with this activity.
    /// </summary>
    [JsonPropertyName("channelData")]
    public new TeamsChannelData? ChannelData
    {
        get => base.ChannelData as TeamsChannelData;
        set => base.ChannelData = value;
    }

    /// <summary>
    /// Gets or sets the entities specific to Teams.
    /// </summary>
    [JsonPropertyName("entities")] public new EntityList? Entities { get; set; }

    /// <summary>
    /// Attachments specific to Teams.
    /// </summary>
    [JsonPropertyName("attachments")] public new IList<TeamsAttachment>? Attachments { get; set; }

    /// <summary>
    /// Adds an entity to the activity's Entities collection.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public TeamsActivity AddEntity(Entity entity)
    {
        // TODO: Pick up nuances about entities.
        // For eg, there can only be 1 single MessageEntity
        Entities ??= [];
        Entities.Add(entity);
        return this;
    }

    /// <summary>
    /// Creates a new TeamsActivityBuilder instance for building a TeamsActivity with a fluent API.
    /// </summary>
    /// <returns>A new TeamsActivityBuilder instance.</returns>
    public static new TeamsActivityBuilder CreateBuilder() => new();

    /// <summary>
    /// Creates a new TeamsActivityBuilder instance initialized with the specified TeamsActivity.
    /// </summary>
    /// <param name="activity"></param>
    /// <returns></returns>
    public static TeamsActivityBuilder CreateBuilder(TeamsActivity activity) => new(activity);

}
