// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema.ConversationActivities;

/// <summary>
/// Represents a conversation update activity.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227: Collection Properties should be read only", Justification = "<Pending>")]
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
    public ConversationUpdateActivity() : base(TeamsActivityType.ConversationUpdate)
    {
    }

    /// <summary>
    /// Internal constructor to create ConversationUpdateActivity from CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    protected ConversationUpdateActivity(CoreActivity activity) : base(activity)
    {
        if (activity.Properties.TryGetValue("topicName", out var topicName))
        {
            TopicName = topicName?.ToString();
            activity.Properties.Remove("topicName");
        }

        if (activity.Properties.TryGetValue("historyDisclosed", out var historyDisclosed) && historyDisclosed != null)
        {
            if (historyDisclosed is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.True)
                    HistoryDisclosed = true;
                else if (je.ValueKind == JsonValueKind.False)
                    HistoryDisclosed = false;
            }
            else if (historyDisclosed is bool boolValue)
            {
                HistoryDisclosed = boolValue;
            }
            else if (bool.TryParse(historyDisclosed.ToString(), out var result))
            {
                HistoryDisclosed = result;
            }
            activity.Properties.Remove("historyDisclosed");
        }

        if (activity.Properties.TryGetValue("membersAdded", out var membersAdded) && membersAdded != null)
        {
            if (membersAdded is JsonElement je)
            {
                MembersAdded = JsonSerializer.Deserialize<IList<TeamsConversationAccount>>(je.GetRawText());
            }
            else
            {
                MembersAdded = membersAdded as IList<TeamsConversationAccount>;
            }
            activity.Properties.Remove("membersAdded");
        }

        if (activity.Properties.TryGetValue("membersRemoved", out var membersRemoved) && membersRemoved != null)
        {
            if (membersRemoved is JsonElement je)
            {
                MembersRemoved = JsonSerializer.Deserialize<IList<TeamsConversationAccount>>(je.GetRawText());
            }
            else
            {
                MembersRemoved = membersRemoved as IList<TeamsConversationAccount>;
            }
            activity.Properties.Remove("membersRemoved");
        }
    }

    /// <summary>
    /// Gets or sets the updated topic name of the conversation.
    /// </summary>
    [JsonPropertyName("topicName")]
    public string? TopicName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the prior history is disclosed.
    /// </summary>
    [JsonPropertyName("historyDisclosed")]
    public bool? HistoryDisclosed { get; set; }

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
    public const string ChannelUnShared = "channelUnShared";

    /// <summary>
    /// Channel member added event.
    /// </summary>
    public const string ChannelMemberAdded = "channelMemberAdded";

    /// <summary>
    /// Channel member removed event.
    /// </summary>
    public const string ChannelMemberRemoved = "channelMemberRemoved";

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
