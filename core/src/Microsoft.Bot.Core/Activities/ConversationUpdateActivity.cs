// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Conversation update event type constants.
/// </summary>
public static class ConversationUpdateEventTypes
{
    /// <summary>
    /// Channel created event.
    /// </summary>
    public const string ChannelCreated = "channelCreated";

    /// <summary>
    /// Channel deleted event.
    /// </summary>
    public const string ChannelDeleted = "channelDeleted";

    /// <summary>
    /// Channel renamed event.
    /// </summary>
    public const string ChannelRenamed = "channelRenamed";

    /// <summary>
    /// Channel restored event.
    /// </summary>
    public const string ChannelRestored = "channelRestored";

    /// <summary>
    /// Channel shared event.
    /// </summary>
    public const string ChannelShared = "channelShared";

    /// <summary>
    /// Channel unshared event.
    /// </summary>
    public const string ChannelUnShared = "channelUnshared";

    /// <summary>
    /// Channel member added event.
    /// </summary>
    public const string ChannelMemberAdded = "channelMemberAdded";

    /// <summary>
    /// Channel member removed event.
    /// </summary>
    public const string ChannelMemberRemoved = "channelMemberRemoved";

    /// <summary>
    /// Team archived event.
    /// </summary>
    public const string TeamArchived = "teamArchived";

    /// <summary>
    /// Team deleted event.
    /// </summary>
    public const string TeamDeleted = "teamDeleted";

    /// <summary>
    /// Team hard deleted event.
    /// </summary>
    public const string TeamHardDeleted = "teamHardDeleted";

    /// <summary>
    /// Team renamed event.
    /// </summary>
    public const string TeamRenamed = "teamRenamed";

    /// <summary>
    /// Team restored event.
    /// </summary>
    public const string TeamRestored = "teamRestored";

    /// <summary>
    /// Team unarchived event.
    /// </summary>
    public const string TeamUnarchived = "teamUnarchived";
}

/// <summary>
/// Represents a conversation update activity.
/// </summary>
public class ConversationUpdateActivity : Activity
{
    /// <summary>
    /// Gets or sets the updated topic name of the conversation.
    /// </summary>
    public string? TopicName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the prior history of the channel is disclosed.
    /// </summary>
    public bool? HistoryDisclosed { get; set; }

    /// <summary>
    /// Gets or sets the collection of members added to the conversation.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Matches source API structure for serialization")]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Matches source API structure")]
    public BotAccount[]? MembersAdded { get; set; }

    /// <summary>
    /// Gets or sets the collection of members removed from the conversation.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Matches source API structure for serialization")]
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Matches source API structure")]
    public BotAccount[]? MembersRemoved { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationUpdateActivity"/> class.
    /// </summary>
    public ConversationUpdateActivity() : base(ActivityTypes.ConversationUpdate)
    {
    }
}
