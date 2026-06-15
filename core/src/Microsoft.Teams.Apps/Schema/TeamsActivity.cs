// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Teams Activity schema.
/// </summary>
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

        return TeamsActivityType.ActivityDeserializerMap.TryGetValue(activity.Type, out Func<CoreActivity, TeamsActivity>? factory)
            ? factory(activity)
            : new TeamsActivity(activity);  // Fallback to base type
    }

    /// <summary>
    /// Overrides the ToJson method to serialize the TeamsActivity object to a JSON string.
    /// Uses the appropriate JSON type info based on the actual runtime type.
    /// </summary>
    /// <returns>A JSON string representation of the activity using the type-specific serializer.</returns>
    public override string ToJson()
        => TeamsActivityType.ActivitySerializerMap.TryGetValue(GetType(), out Func<TeamsActivity, string>? serializer)
            ? serializer(this)
            : ToJson(TeamsActivityJsonContext.Default.TeamsActivity);

    /// <summary>
    /// Constructor with type parameter.
    /// </summary>
    /// <param name="type"></param>
    protected TeamsActivity(string type) : this()
    {
        Type = type;
    }

    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    public TeamsActivity()
    {
        Type = TeamsActivityType.Message;
    }

    /// <summary>
    /// Protected constructor to create TeamsActivity from CoreActivity.
    /// Allows derived classes to call via base(activity).
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    protected TeamsActivity(CoreActivity activity) : base(activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        // Convert core extension properties to Teams-specific typed properties.
        // CoreActivity stores these as untyped entries in its Properties dictionary
        // (via [JsonExtensionData]), so we extract and promote them here.
        base.From = TeamsChannelAccount.FromChannelAccount(activity.From) ?? new TeamsChannelAccount();
        base.Recipient = TeamsChannelAccount.FromChannelAccount(activity.Recipient) ?? new TeamsChannelAccount();
        base.Conversation = TeamsConversation.FromConversation(activity.Conversation) ?? new TeamsConversation();
        ChannelData = activity.Properties.Extract<TeamsChannelData>("channelData");
        Entities = activity.Properties.Extract<EntityList>("entities");
    }

    /// <summary>
    /// Gets or sets the account information for the sender of the Teams conversation.
    /// Delegates to the base CoreActivity.From slot, casting to TeamsChannelAccount.
    /// </summary>
    [JsonPropertyName("from")]
    public new TeamsChannelAccount? From
    {
        get => base.From as TeamsChannelAccount ?? TeamsChannelAccount.FromChannelAccount(base.From);
        set => base.From = value;
    }

    /// <summary>
    /// Gets or sets the account information for the recipient of the Teams conversation.
    /// Delegates to the base CoreActivity.Recipient slot, casting to TeamsChannelAccount.
    /// </summary>
    [JsonPropertyName("recipient")]
    public new TeamsChannelAccount? Recipient
    {
        get => base.Recipient as TeamsChannelAccount ?? TeamsChannelAccount.FromChannelAccount(base.Recipient);
        set => base.Recipient = value;
    }

    /// <summary>
    /// Gets or sets the conversation information for the Teams conversation.
    /// Delegates to the base CoreActivity.Conversation slot, casting to TeamsConversation.
    /// </summary>
    [JsonPropertyName("conversation")]
    public new TeamsConversation? Conversation
    {
        get => base.Conversation as TeamsConversation ?? TeamsConversation.FromConversation(base.Conversation);
        set => base.Conversation = value!;
    }

    /// <summary>
    /// Gets or sets the Teams-specific channel data associated with this activity.
    /// </summary>
    [JsonPropertyName("channelData")]
    public TeamsChannelData? ChannelData { get; set; }

    /// <summary>
    /// Gets or sets the entities specific to Teams.
    /// </summary>
    [JsonPropertyName("entities")]
    public EntityList? Entities { get; set; }

    /// <summary>
    /// UTC timestamp of when the activity was sent.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    /// <summary>
    /// Local timestamp of when the activity was sent, including timezone offset.
    /// </summary>
    [JsonPropertyName("localTimestamp")]
    public string? LocalTimestamp { get; set; }

    /// <summary>
    /// Locale of the activity set by the client (e.g., "en-US").
    /// </summary>
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    /// <summary>
    /// Local timezone of the client (e.g., "America/Los_Angeles").
    /// </summary>
    [JsonPropertyName("localTimezone")]
    public string? LocalTimezone { get; set; }

    /// <summary>
    /// Gets or sets the suggested actions for the message.
    /// </summary>
    [JsonPropertyName("suggestedActions")]
    public SuggestedActions? SuggestedActions { get; set; }

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
