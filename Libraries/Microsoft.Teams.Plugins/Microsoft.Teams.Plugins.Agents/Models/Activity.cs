using System.Text.Json;

using Microsoft.Teams.Api.Activities;

using Types = Microsoft.Agents.Core.Models;

namespace Microsoft.Teams.Plugins.Agents.Models;

public static partial class AgentExtensions
{
    public static IActivity ToTeamsEntity(this Types.IActivity activity)
    {
        var type = new ActivityType(activity.Type);

        if (type.IsMessage && activity is Types.IMessageActivity message)
        {
            return message.ToTeamsEntity();
        }
        else if (type.IsMessageUpdate && activity is Types.IMessageUpdateActivity messageUpdate)
        {
            return messageUpdate.ToTeamsEntity();
        }
        else if (type.IsMessageReaction && activity is Types.IMessageReactionActivity messageReaction)
        {
            return messageReaction.ToTeamsEntity();
        }
        else if (type.IsMessageDelete && activity is Types.IMessageDeleteActivity messageDelete)
        {
            return messageDelete.ToTeamsEntity();
        }

        return new Activity(activity.Type)
        {
            Id = activity.Id,
            Timestamp = activity.Timestamp?.DateTime,
            LocalTimestamp = activity.LocalTimestamp?.DateTime,
            ChannelId = new(activity.ChannelId.Channel),
            From = activity.From.ToTeamsEntity(),
            Conversation = activity.Conversation.ToTeamsEntity(),
            Recipient = activity.Recipient.ToTeamsEntity(),
            ChannelData = activity.ChannelData.ToTeamsChannelData(),
            Entities = activity.Entities.Select(e => e.ToTeamsEntity()).ToList(),
            Locale = activity.Locale,
            RelatesTo = activity.RelatesTo.ToTeamsEntity(),
            ReplyToId = activity.ReplyToId,
            ServiceUrl = activity.ServiceUrl,
            Properties = activity.Properties.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Deserialize<object>()
            )
        };
    }
}

public static partial class AgentExtensions
{
    public static Types.IActivity ToAgentEntity(this IActivity activity)
    {
        if (activity is MessageActivity message)
        {
            return message.ToAgentEntity();
        }
        else if (activity is MessageUpdateActivity messageUpdate)
        {
            return messageUpdate.ToAgentEntity();
        }
        else if (activity is MessageReactionActivity messageReaction)
        {
            return messageReaction.ToAgentEntity();
        }
        else if (activity is MessageDeleteActivity messageDelete)
        {
            return messageDelete.ToAgentEntity();
        }

        return new Types.Activity(activity.Type)
        {
            Id = activity.Id,
            Timestamp = activity.Timestamp,
            LocalTimestamp = activity.LocalTimestamp,
            ChannelId = new(activity.ChannelId),
            From = activity.From.ToAgentEntity(),
            Conversation = activity.Conversation.ToAgentEntity(),
            Recipient = activity.Recipient.ToAgentEntity(),
            ChannelData = activity.ChannelData?.ToAgentChannelData(),
            Entities = activity.Entities?.Select(e => e.ToAgentEntity()).ToList(),
            Locale = activity.Locale,
            RelatesTo = activity.RelatesTo?.ToAgentEntity(),
            ReplyToId = activity.ReplyToId,
            ServiceUrl = activity.ServiceUrl,
            Properties = activity.Properties?.ToDictionary(
                pair => pair.Key,
                pair => JsonSerializer.SerializeToElement(pair.Value)
            )
        };
    }
}