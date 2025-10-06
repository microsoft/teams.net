using System.Text.Json;

using Microsoft.Teams.Api.Activities;

using Types = Microsoft.Agents.Core.Models;

namespace Microsoft.Teams.Plugins.Agents.Models;

public static partial class AgentExtensions
{
    public static MessageActivity ToTeamsEntity(this Types.IMessageActivity activity)
    {
        return new()
        {
            Id = activity.Id,
            Timestamp = activity.Timestamp?.DateTime,
            LocalTimestamp = activity.LocalTimestamp?.DateTime,
            ChannelId = new(activity.ChannelId.Channel),
            From = activity.From.ToTeamsEntity(),
            Conversation = activity.Conversation.ToTeamsEntity(),
            Recipient = activity.Recipient.ToTeamsEntity(),
            TextFormat = new(activity.TextFormat),
            AttachmentLayout = new(activity.AttachmentLayout),
            Attachments = activity.Attachments?.Select(a => a.ToTeamsEntity()).ToList(),
            ChannelData = activity.ChannelData?.ToTeamsChannelData(),
            DeliveryMode = new(activity.DeliveryMode),
            Entities = activity.Entities?.Select(e => e.ToTeamsEntity()).ToList(),
            Expiration = activity.Expiration?.DateTime,
            Importance = new(activity.Importance),
            InputHint = new(activity.InputHint),
            Locale = activity.Locale,
            RelatesTo = activity.RelatesTo?.ToTeamsEntity(),
            ReplyToId = activity.ReplyToId,
            ServiceUrl = activity.ServiceUrl,
            Speak = activity.Speak,
            Summary = activity.Summary,
            Text = activity.Text,
            Value = activity.Value,
            SuggestedActions = activity.SuggestedActions?.ToTeamsEntity(),
            Properties = activity.Properties?.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Deserialize<object>()
            )
        };
    }

    public static MessageUpdateActivity ToTeamsEntity(this Types.IMessageUpdateActivity activity)
    {
        return new()
        {
            Id = activity.Id,
            Timestamp = activity.Timestamp?.DateTime,
            LocalTimestamp = activity.LocalTimestamp?.DateTime,
            ChannelId = new(activity.ChannelId.Channel),
            From = activity.From.ToTeamsEntity(),
            Conversation = activity.Conversation.ToTeamsEntity(),
            Recipient = activity.Recipient.ToTeamsEntity(),
            TextFormat = new(activity.TextFormat),
            AttachmentLayout = new(activity.AttachmentLayout),
            Attachments = activity.Attachments.Select(a => a.ToTeamsEntity()).ToList(),
            ChannelData = activity.ChannelData.ToTeamsChannelData(),
            DeliveryMode = new(activity.DeliveryMode),
            Entities = activity.Entities.Select(e => e.ToTeamsEntity()).ToList(),
            Expiration = activity.Expiration?.DateTime,
            Importance = new(activity.Importance),
            InputHint = new(activity.InputHint),
            Locale = activity.Locale,
            RelatesTo = activity.RelatesTo?.ToTeamsEntity(),
            ReplyToId = activity.ReplyToId,
            ServiceUrl = activity.ServiceUrl,
            Speak = activity.Speak,
            Summary = activity.Summary,
            Text = activity.Text,
            Value = activity.Value,
            SuggestedActions = activity.SuggestedActions.ToTeamsEntity(),
            Properties = activity.Properties.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Deserialize<object>()
            )
        };
    }

    public static MessageReactionActivity ToTeamsEntity(this Types.IMessageReactionActivity activity)
    {
        return new()
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
            RelatesTo = activity.RelatesTo?.ToTeamsEntity(),
            ReplyToId = activity.ReplyToId,
            ServiceUrl = activity.ServiceUrl,
            ReactionsAdded = activity.ReactionsAdded.Select(r => r.ToTeamsEntity()).ToList(),
            ReactionsRemoved = activity.ReactionsRemoved.Select(r => r.ToTeamsEntity()).ToList(),
            Properties = activity.Properties.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Deserialize<object>()
            )
        };
    }

    public static MessageDeleteActivity ToTeamsEntity(this Types.IMessageDeleteActivity activity)
    {
        return new()
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
            RelatesTo = activity.RelatesTo?.ToTeamsEntity(),
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
    public static Types.IMessageActivity ToAgentEntity(this MessageActivity activity)
    {
        return new Types.Activity(activity.Type)
        {
            Id = activity.Id,
            Timestamp = activity.Timestamp,
            LocalTimestamp = activity.LocalTimestamp,
            ChannelId = new(activity.ChannelId),
            From = activity.From.ToAgentEntity(),
            Conversation = activity.Conversation.ToAgentEntity(),
            Recipient = activity.Recipient.ToAgentEntity(),
            TextFormat = activity.TextFormat?.ToString(),
            AttachmentLayout = activity.AttachmentLayout?.ToString(),
            Attachments = activity.Attachments?.Select(a => a.ToAgentEntity()).ToList(),
            ChannelData = activity.ChannelData?.ToAgentChannelData(),
            DeliveryMode = activity.DeliveryMode?.ToString(),
            Entities = activity.Entities?.Select(e => e.ToAgentEntity()).ToList(),
            Expiration = activity.Expiration,
            Importance = activity.Importance?.ToString(),
            InputHint = activity.InputHint?.ToString(),
            Locale = activity.Locale,
            RelatesTo = activity.RelatesTo?.ToAgentEntity(),
            ReplyToId = activity.ReplyToId,
            ServiceUrl = activity.ServiceUrl,
            Speak = activity.Speak,
            Summary = activity.Summary,
            Text = activity.Text,
            Value = activity.Value,
            SuggestedActions = activity.SuggestedActions?.ToAgentEntity(),
            Properties = activity.Properties?.ToDictionary(
                pair => pair.Key,
                pair => JsonSerializer.SerializeToElement(pair.Value)
            )
        };
    }

    public static Types.IMessageUpdateActivity ToAgentEntity(this MessageUpdateActivity activity)
    {
        return new Types.Activity(activity.Type)
        {
            Id = activity.Id,
            Timestamp = activity.Timestamp,
            LocalTimestamp = activity.LocalTimestamp,
            ChannelId = new(activity.ChannelId),
            From = activity.From.ToAgentEntity(),
            Conversation = activity.Conversation.ToAgentEntity(),
            Recipient = activity.Recipient.ToAgentEntity(),
            TextFormat = activity.TextFormat?.ToString(),
            AttachmentLayout = activity.AttachmentLayout?.ToString(),
            Attachments = activity.Attachments?.Select(a => a.ToAgentEntity()).ToList(),
            ChannelData = activity.ChannelData?.ToAgentChannelData(),
            DeliveryMode = activity.DeliveryMode?.ToString(),
            Entities = activity.Entities?.Select(e => e.ToAgentEntity()).ToList(),
            Expiration = activity.Expiration,
            Importance = activity.Importance?.ToString(),
            InputHint = activity.InputHint?.ToString(),
            Locale = activity.Locale,
            RelatesTo = activity.RelatesTo?.ToAgentEntity(),
            ReplyToId = activity.ReplyToId,
            ServiceUrl = activity.ServiceUrl,
            Speak = activity.Speak,
            Summary = activity.Summary,
            Text = activity.Text,
            Value = activity.Value,
            SuggestedActions = activity.SuggestedActions?.ToAgentEntity(),
            Properties = activity.Properties?.ToDictionary(
                pair => pair.Key,
                pair => JsonSerializer.SerializeToElement(pair.Value)
            )
        };
    }

    public static Types.IMessageReactionActivity ToAgentEntity(this MessageReactionActivity activity)
    {
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
            ReactionsAdded = activity.ReactionsAdded?.Select(r => r.ToAgentEntity()).ToList(),
            ReactionsRemoved = activity.ReactionsRemoved?.Select(r => r.ToAgentEntity()).ToList(),
            Properties = activity.Properties?.ToDictionary(
                pair => pair.Key,
                pair => JsonSerializer.SerializeToElement(pair.Value)
            )
        };
    }

    public static Types.IMessageDeleteActivity ToAgentEntity(this MessageDeleteActivity activity)
    {
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