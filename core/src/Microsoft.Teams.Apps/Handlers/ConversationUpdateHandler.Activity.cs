// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Utils;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Represents a conversation update activity.
/// </summary>
public class ConversationUpdateActivity : TeamsActivity
{
    /// <summary>
    /// Convenience method to create a ConversationUpdateActivity from a CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    /// <returns>A ConversationUpdateActivity instance.</returns>
    public static new ConversationUpdateActivity FromActivity(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return new ConversationUpdateActivity(activity);
    }

    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    internal ConversationUpdateActivity() : base(TeamsActivityTypes.ConversationUpdate)
    {
    }

    /// <summary>
    /// Internal constructor to create ConversationUpdateActivity from CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    internal ConversationUpdateActivity(CoreActivity activity) : base(activity)
    {
        /*
        if (activity.Properties.TryGetValue("topicName", out var topicName))
        {
            TopicName = topicName?.ToString();
            activity.Properties.Remove("topicName");
        }
        */

        MembersAdded = Properties.Extract<IList<TeamsChannelAccount>>("membersAdded");
        MembersRemoved = Properties.Extract<IList<TeamsChannelAccount>>("membersRemoved");
    }

    //TODO : review properties
    /*
    /// <summary>
    /// Gets or sets the updated topic name of the conversation.
    /// </summary>
    [JsonPropertyName("topicName")]
    public string? TopicName { get; internal set; }
    */

    /// <summary>
    /// Gets or sets the collection of members added to the conversation.
    /// </summary>
    [JsonPropertyName("membersAdded")]
    public IList<TeamsChannelAccount>? MembersAdded { get; internal set; }

    /// <summary>
    /// Gets or sets the collection of members removed from the conversation.
    /// </summary>
    [JsonPropertyName("membersRemoved")]
    public IList<TeamsChannelAccount>? MembersRemoved { get; internal set; }
}

/// <summary>
/// String enum for conversation event types.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<ConversationEventType>))]
public class ConversationEventType(string value) : StringEnum(value)
{
    /// <summary>Gets the channel created event type.</summary>
    public static readonly ConversationEventType ChannelCreated = new("channelCreated");
    /// <summary>Gets the channel deleted event type.</summary>
    public static readonly ConversationEventType ChannelDeleted = new("channelDeleted");
    /// <summary>Gets the channel renamed event type.</summary>
    public static readonly ConversationEventType ChannelRenamed = new("channelRenamed");
    /// <summary>Gets the channel shared event type.</summary>
    public static readonly ConversationEventType ChannelShared = new("channelShared");
    /// <summary>Gets the channel unshared event type.</summary>
    public static readonly ConversationEventType ChannelUnShared = new("channelUnshared");
    /// <summary>Gets the channel member added event type.</summary>
    public static readonly ConversationEventType ChannelMemberAdded = new("channelMemberAdded");
    /// <summary>Gets the channel member removed event type.</summary>
    public static readonly ConversationEventType ChannelMemberRemoved = new("channelMemberRemoved");
    /// <summary>Gets the team member added event type.</summary>
    public static readonly ConversationEventType TeamMemberAdded = new("teamMemberAdded");
    /// <summary>Gets the team member removed event type.</summary>
    public static readonly ConversationEventType TeamMemberRemoved = new("teamMemberRemoved");
    /// <summary>Gets the team archived event type.</summary>
    public static readonly ConversationEventType TeamArchived = new("teamArchived");
    /// <summary>Gets the team deleted event type.</summary>
    public static readonly ConversationEventType TeamDeleted = new("teamDeleted");
    /// <summary>Gets the team renamed event type.</summary>
    public static readonly ConversationEventType TeamRenamed = new("teamRenamed");
    /// <summary>Gets the team unarchived event type.</summary>
    public static readonly ConversationEventType TeamUnarchived = new("teamUnarchived");

}

/// <summary>
/// Common conversation event type values.
/// </summary>
public static class ConversationEventTypes
{
    /// <summary>Gets the channel created event type.</summary>
    public static ConversationEventType ChannelCreated => ConversationEventType.ChannelCreated;

    /// <summary>Gets the channel deleted event type.</summary>
    public static ConversationEventType ChannelDeleted => ConversationEventType.ChannelDeleted;

    /// <summary>Gets the channel renamed event type.</summary>
    public static ConversationEventType ChannelRenamed => ConversationEventType.ChannelRenamed;

    /// <summary>Gets the channel shared event type.</summary>
    public static ConversationEventType ChannelShared => ConversationEventType.ChannelShared;

    /// <summary>Gets the channel unshared event type.</summary>
    public static ConversationEventType ChannelUnShared => ConversationEventType.ChannelUnShared;

    /// <summary>Gets the channel member added event type.</summary>
    public static ConversationEventType ChannelMemberAdded => ConversationEventType.ChannelMemberAdded;

    /// <summary>Gets the channel member removed event type.</summary>
    public static ConversationEventType ChannelMemberRemoved => ConversationEventType.ChannelMemberRemoved;

    /// <summary>Gets the team member added event type.</summary>
    public static ConversationEventType TeamMemberAdded => ConversationEventType.TeamMemberAdded;

    /// <summary>Gets the team member removed event type.</summary>
    public static ConversationEventType TeamMemberRemoved => ConversationEventType.TeamMemberRemoved;

    /// <summary>Gets the team archived event type.</summary>
    public static ConversationEventType TeamArchived => ConversationEventType.TeamArchived;

    /// <summary>Gets the team deleted event type.</summary>
    public static ConversationEventType TeamDeleted => ConversationEventType.TeamDeleted;

    /// <summary>Gets the team renamed event type.</summary>
    public static ConversationEventType TeamRenamed => ConversationEventType.TeamRenamed;

    /// <summary>Gets the team unarchived event type.</summary>
    public static ConversationEventType TeamUnarchived => ConversationEventType.TeamUnarchived;
}
