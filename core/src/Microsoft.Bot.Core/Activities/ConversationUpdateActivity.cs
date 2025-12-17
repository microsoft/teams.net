// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a conversation update activity.
/// </summary>
public class ConversationUpdateActivity : Activity
{
    /// <summary>
    /// Gets or sets the updated topic name of the conversation.
    /// </summary>
    [JsonPropertyName("topicName")]
    public string? TopicName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the prior history of the channel is disclosed.
    /// </summary>
    [JsonPropertyName("historyDisclosed")]
    public bool? HistoryDisclosed { get; set; }

    /// <summary>
    /// Gets or sets the collection of members added to the conversation.
    /// </summary>
    [JsonPropertyName("membersAdded")]
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<Account>? MembersAdded { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

    /// <summary>
    /// Gets or sets the collection of members removed from the conversation.
    /// </summary>
    [JsonPropertyName("membersRemoved")]
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<Account>? MembersRemoved { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationUpdateActivity"/> class.
    /// </summary>
    public ConversationUpdateActivity() : base(ActivityTypes.ConversationUpdate)
    {
    }
}

/// <summary>
/// String constants for conversation update event types.
/// </summary>
public static class ConversationEventTypes
{
    /// <summary>
    /// Channel created event type.
    /// </summary>
    public const string ChannelCreated = "channelCreated";

    /// <summary>
    /// Channel deleted event type.
    /// </summary>
    public const string ChannelDeleted = "channelDeleted";

    /// <summary>
    /// Channel renamed event type.
    /// </summary>
    public const string ChannelRenamed = "channelRenamed";

    /// <summary>
    /// Channel restored event type.
    /// </summary>
    public const string ChannelRestored = "channelRestored";

    /// <summary>
    /// Channel shared event type.
    /// </summary>
    public const string ChannelShared = "channelShared";

    /// <summary>
    /// Channel unshared event type.
    /// </summary>
    public const string ChannelUnShared = "channelUnshared";

    /// <summary>
    /// Channel member added event type.
    /// </summary>
    public const string ChannelMemberAdded = "channelMemberAdded";

    /// <summary>
    /// Channel member removed event type.
    /// </summary>
    public const string ChannelMemberRemoved = "channelMemberRemoved";

    /// <summary>
    /// Team archived event type.
    /// </summary>
    public const string TeamArchived = "teamArchived";

    /// <summary>
    /// Team deleted event type.
    /// </summary>
    public const string TeamDeleted = "teamDeleted";

    /// <summary>
    /// Team hard deleted event type.
    /// </summary>
    public const string TeamHardDeleted = "teamHardDeleted";

    /// <summary>
    /// Team renamed event type.
    /// </summary>
    public const string TeamRenamed = "teamRenamed";

    /// <summary>
    /// Team restored event type.
    /// </summary>
    public const string TeamRestored = "teamRestored";

    /// <summary>
    /// Team unarchived event type.
    /// </summary>
    public const string TeamUnarchived = "teamUnarchived";
}
