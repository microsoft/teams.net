// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities;

public partial class ActivityType : StringEnum
{
    public static readonly ActivityType ConversationUpdate = new("conversationUpdate");
    public bool IsConversationUpdate => ConversationUpdate.Equals(Value);
}

public class ConversationUpdateActivity() : Activity(ActivityType.ConversationUpdate)
{
    /// <summary>
    /// The updated topic name of the conversation.
    /// </summary>
    [JsonPropertyName("topicName")]
    [JsonPropertyOrder(31)]
    public string? TopicName { get; set; }

    /// <summary>
    /// Indicates whether the prior history of the channel is disclosed.
    /// </summary>
    [JsonPropertyName("historyDisclosed")]
    [JsonPropertyOrder(32)]
    public bool? HistoryDisclosed { get; set; }

    /// <summary>
    /// The collection of members added to the conversation.
    /// </summary>
    [JsonPropertyName("membersAdded")]
    [JsonPropertyOrder(33)]
    public Account[] MembersAdded { get; set; } = [];

    /// <summary>
    /// The collection of members removed from the conversation.
    /// </summary>
    [JsonPropertyName("membersRemoved")]
    [JsonPropertyOrder(34)]
    public Account[] MembersRemoved { get; set; } = [];

    [JsonConverter(typeof(JsonConverter<EventType>))]
    public class EventType(string value) : StringEnum(value)
    {
        public static readonly EventType ChannelCreated = new("channelCreated");
        public bool IsChannelCreated => ChannelCreated.Equals(Value);

        public static readonly EventType ChannelDeleted = new("channelDeleted");
        public bool IsChannelDeleted => ChannelDeleted.Equals(Value);

        public static readonly EventType ChannelRenamed = new("channelRenamed");
        public bool IsChannelRenamed => ChannelRenamed.Equals(Value);

        public static readonly EventType ChannelRestored = new("channelRestored");
        public bool IsChannelRestored => ChannelRestored.Equals(Value);

        public static readonly EventType TeamArchived = new("teamArchived");
        public bool IsTeamArchived => TeamArchived.Equals(Value);

        public static readonly EventType TeamDeleted = new("teamDeleted");
        public bool IsTeamDeleted => TeamDeleted.Equals(Value);

        public static readonly EventType TeamHardDeleted = new("teamHardDeleted");
        public bool IsTeamHardDeleted => TeamHardDeleted.Equals(Value);

        public static readonly EventType TeamRenamed = new("teamRenamed");
        public bool IsTeamRenamed => TeamRenamed.Equals(Value);

        public static readonly EventType TeamRestored = new("teamRestored");
        public bool IsTeamRestored => TeamRestored.Equals(Value);

        public static readonly EventType TeamUnarchived = new("teamUnarchived");
        public bool IsTeamUnarchived => TeamUnarchived.Equals(Value);
    }
}