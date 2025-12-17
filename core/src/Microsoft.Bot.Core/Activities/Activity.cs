// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Activity type constants.
/// </summary>
public static class ActivityTypes
{
    /// <summary>
    /// Message activity type.
    /// </summary>
    public const string Message = "message";

    /// <summary>
    /// Typing activity type.
    /// </summary>
    public const string Typing = "typing";

    /// <summary>
    /// Command activity type.
    /// </summary>
    public const string Command = "command";

    /// <summary>
    /// Command result activity type.
    /// </summary>
    public const string CommandResult = "commandResult";

    /// <summary>
    /// Conversation update activity type.
    /// </summary>
    public const string ConversationUpdate = "conversationUpdate";

    /// <summary>
    /// End of conversation activity type.
    /// </summary>
    public const string EndOfConversation = "endOfConversation";

    /// <summary>
    /// Install update activity type.
    /// </summary>
    public const string InstallUpdate = "installUpdate";

    /// <summary>
    /// Message update activity type.
    /// </summary>
    public const string MessageUpdate = "messageUpdate";

    /// <summary>
    /// Message delete activity type.
    /// </summary>
    public const string MessageDelete = "messageDelete";

    /// <summary>
    /// Message reaction activity type.
    /// </summary>
    public const string MessageReaction = "messageReaction";

    /// <summary>
    /// Event activity type.
    /// </summary>
    public const string Event = "event";

    /// <summary>
    /// Invoke activity type.
    /// </summary>
    public const string Invoke = "invoke";
}

/// <summary>
/// Channel identifier constants.
/// </summary>
public static class ChannelIds
{
    /// <summary>
    /// Microsoft Teams channel identifier.
    /// </summary>
    public const string MsTeams = "msteams";

    /// <summary>
    /// Web chat channel identifier.
    /// </summary>
    public const string WebChat = "webchat";
}

/// <summary>
/// Represents a bot or user account.
/// </summary>
public class BotAccount
{
    /// <summary>
    /// Gets or sets the account identifier.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the Azure Active Directory object identifier.
    /// </summary>
    public string? AadObjectId { get; set; }

    /// <summary>
    /// Gets or sets the role of the account (e.g., "bot", "user").
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Gets or sets the display name of the account.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets additional properties.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Matches source API structure for serialization")]
    public Dictionary<string, object?>? Properties { get; set; }
}

/// <summary>
/// Represents a conversation.
/// </summary>
public class Conversation
{
    /// <summary>
    /// Gets or sets the conversation identifier.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the conversation type (e.g., "personal", "groupChat", "channel").
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the conversation name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a group conversation.
    /// </summary>
    public bool? IsGroup { get; set; }

    /// <summary>
    /// Gets or sets the list of members in the conversation.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Matches source API structure for serialization")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Matches source API structure")]
    public List<BotAccount>? Members { get; set; }
}

/// <summary>
/// An object relating to a particular point in a conversation.
/// </summary>
public class ConversationReference
{
    /// <summary>
    /// Gets or sets the ID of the activity to refer to.
    /// </summary>
    public string? ActivityId { get; set; }

    /// <summary>
    /// Gets or sets the user participating in this conversation.
    /// </summary>
    public BotAccount? User { get; set; }

    /// <summary>
    /// Gets or sets the locale name for the contents.
    /// </summary>
    public string? Locale { get; set; }

    /// <summary>
    /// Gets or sets the bot participating in this conversation.
    /// </summary>
    public BotAccount? Bot { get; set; }

    /// <summary>
    /// Gets or sets the conversation.
    /// </summary>
    public Conversation? Conversation { get; set; }

    /// <summary>
    /// Gets or sets the channel identifier.
    /// </summary>
    public string? ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the service endpoint URL.
    /// </summary>
    public Uri? ServiceUrl { get; set; }
}

/// <summary>
/// Channel data specific to messages received in Microsoft Teams.
/// </summary>
public class ChannelData
{
    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public string? EventType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the feedback loop is enabled.
    /// </summary>
    public bool? FeedbackLoopEnabled { get; set; }

    /// <summary>
    /// Gets or sets the stream identifier.
    /// </summary>
    public string? StreamId { get; set; }

    /// <summary>
    /// Gets or sets the stream type.
    /// </summary>
    public string? StreamType { get; set; }

    /// <summary>
    /// Gets or sets the stream sequence number.
    /// </summary>
    public int? StreamSequence { get; set; }

    /// <summary>
    /// Gets or sets additional properties.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Matches source API structure for serialization")]
    public Dictionary<string, object?>? Properties { get; set; }
}

/// <summary>
/// Represents a bot activity.
/// </summary>
public class Activity
{
    /// <summary>
    /// Gets or sets the activity identifier.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the activity type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the activity being replied to.
    /// </summary>
    public string? ReplyToId { get; set; }

    /// <summary>
    /// Gets or sets the channel identifier.
    /// </summary>
    public string? ChannelId { get; set; }

    /// <summary>
    /// Gets or sets the sender of the activity.
    /// </summary>
    public BotAccount? From { get; set; }

    /// <summary>
    /// Gets or sets the recipient of the activity.
    /// </summary>
    public BotAccount? Recipient { get; set; }

    /// <summary>
    /// Gets or sets the conversation.
    /// </summary>
    public Conversation? Conversation { get; set; }

    /// <summary>
    /// Gets or sets a reference to another conversation or activity.
    /// </summary>
    public ConversationReference? RelatesTo { get; set; }

    /// <summary>
    /// Gets or sets the service URL.
    /// </summary>
    public Uri? ServiceUrl { get; set; }

    /// <summary>
    /// Gets or sets the locale.
    /// </summary>
    public string? Locale { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the activity.
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the local timestamp of the activity.
    /// </summary>
    public DateTime? LocalTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the list of entities in the activity.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Matches source API structure for serialization")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Matches source API structure")]
    public List<object?>? Entities { get; set; }

    /// <summary>
    /// Gets or sets channel-specific data.
    /// </summary>
    public ChannelData? ChannelData { get; set; }

    /// <summary>
    /// Gets or sets additional properties.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Matches source API structure for serialization")]
    public Dictionary<string, object?>? Properties { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Activity"/> class.
    /// </summary>
    public Activity()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Activity"/> class with the specified type.
    /// </summary>
    /// <param name="type">The activity type.</param>
    public Activity(string? type) : this()
    {
        Type = type;
    }
}
