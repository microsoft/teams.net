// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Handlers;

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
    public ConversationUpdateActivity() : base(TeamsActivityTypes.ConversationUpdate)
    {
    }

    /// <summary>
    /// Internal constructor to create ConversationUpdateActivity from CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    protected ConversationUpdateActivity(CoreActivity activity) : base(activity)
    {
        /*
        if (activity.Properties.TryGetValue("topicName", out var topicName))
        {
            TopicName = topicName?.ToString();
            activity.Properties.Remove("topicName");
        }
        */

        MembersAdded = activity.Properties.Extract<IList<TeamsConversationAccount>>("membersAdded");
        MembersRemoved = activity.Properties.Extract<IList<TeamsConversationAccount>>("membersRemoved");
    }

    //TODO : review properties
    /*
    /// <summary>
    /// Gets or sets the updated topic name of the conversation.
    /// </summary>
    [JsonPropertyName("topicName")]
    public string? TopicName { get; set; }
    */

    /// <summary>
    /// Gets or sets the collection of members added to the conversation.
    /// </summary>
    [JsonPropertyName("membersAdded")]
    public IList<TeamsConversationAccount>? MembersAdded { get; set; }

    /// <summary>
    /// Gets or sets the collection of members removed from the conversation.
    /// </summary>
    [JsonPropertyName("membersRemoved")]
    public IList<TeamsConversationAccount>? MembersRemoved { get; set; }
}

/// <summary>
/// String constants for conversation event types.
/// </summary>
public static class ConversationEventTypes
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

    //TODO : review these events
    /*
    /// <summary>
    /// Channel restored event.
    /// </summary>
    public const string ChannelRestored = "channelRestored";
    */

    /// <summary>
    /// Team member added event.
    /// </summary>
    public const string TeamMemberAdded = "teamMemberAdded";

    /// <summary>
    /// Team member removed event.
    /// </summary>
    public const string TeamMemberRemoved = "teamMemberRemoved";

    /// <summary>
    /// Team archived event.
    /// </summary>
    public const string TeamArchived = "teamArchived";

    /// <summary>
    /// Team deleted event.
    /// </summary>
    public const string TeamDeleted = "teamDeleted";

    /// <summary>
    /// Team renamed event.
    /// </summary>
    public const string TeamRenamed = "teamRenamed";

    /// <summary>
    /// Team unarchived event.
    /// </summary>
    public const string TeamUnarchived = "teamUnarchived";

    /*TODO : review these events
    /// <summary>
    /// Team hard deleted event.
    /// </summary>
    public const string TeamHardDeleted = "teamHardDeleted";

    /// <summary>
    /// Team restored event.
    /// </summary>
    public const string TeamRestored = "teamRestored";
    */
}
