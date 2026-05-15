// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Extension methods for <see cref="TeamsActivity"/>.
/// </summary>
/// <remarks>
/// These methods provide backward compatibility with the old library's fluent activity composition pattern.
/// </remarks>
public static class TeamsActivityExtensions
{
    /// <summary>
    /// Sets the activity id.
    /// </summary>
    public static TeamsActivity WithId(this TeamsActivity activity, string value)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.Id = value;
        return activity;
    }

    /// <summary>
    /// Sets the channel id.
    /// </summary>
    public static TeamsActivity WithChannelId(this TeamsActivity activity, string? value)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ChannelId = value;
        return activity;
    }

    /// <summary>
    /// Sets the sender account.
    /// </summary>
    public static TeamsActivity WithFrom(this TeamsActivity activity, ConversationAccount? value)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.From = value is TeamsConversationAccount teamsAccount
            ? teamsAccount
            : TeamsConversationAccount.FromConversationAccount(value);
        return activity;
    }

    /// <summary>
    /// Sets the conversation information.
    /// </summary>
    public static TeamsActivity WithConversation(this TeamsActivity activity, Conversation? value)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(value);

        activity.Conversation = value is TeamsConversation teamsConversation
            ? teamsConversation
            : TeamsConversation.FromConversation(value);
        return activity;
    }

    /// <summary>
    /// Sets the recipient account.
    /// </summary>
    public static TeamsActivity WithRecipient(this TeamsActivity activity, ConversationAccount? value)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.Recipient = value is TeamsConversationAccount teamsAccount
            ? teamsAccount
            : TeamsConversationAccount.FromConversationAccount(value);
        return activity;
    }

    /// <summary>
    /// Sets the recipient account and targeted visibility flag.
    /// </summary>
    [Experimental("ExperimentalTeamsTargeted")]
    public static TeamsActivity WithRecipient(this TeamsActivity activity, ConversationAccount? value, bool isTargeted)
    {
        ArgumentNullException.ThrowIfNull(activity);

        if (value is not null)
        {
            value.IsTargeted = isTargeted ? true : null;
            activity.Recipient = value is TeamsConversationAccount teamsAccount
                ? teamsAccount
                : TeamsConversationAccount.FromConversationAccount(value);
        }

        return activity;
    }

    /// <summary>
    /// Sets the service url.
    /// </summary>
    public static TeamsActivity WithServiceUrl(this TeamsActivity activity, Uri? value)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.ServiceUrl = value;
        return activity;
    }

    /// <summary>
    /// Sets the locale.
    /// </summary>
    public static TeamsActivity WithLocale(this TeamsActivity activity, string? value)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.Locale = value;
        return activity;
    }

    /// <summary>
    /// Sets the UTC timestamp value.
    /// </summary>
    public static TeamsActivity WithTimestamp(this TeamsActivity activity, string? value)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.Timestamp = value;
        return activity;
    }

    /// <summary>
    /// Sets the local timestamp value.
    /// </summary>
    public static TeamsActivity WithLocalTimestamp(this TeamsActivity activity, string? value)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.LocalTimestamp = value;
        return activity;
    }

    /// <summary>
    /// Merges channel data properties into the activity.
    /// </summary>
    public static TeamsActivity WithData(this TeamsActivity activity, ChannelData value)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(value);

        activity.ChannelData ??= new TeamsChannelData();
        foreach (KeyValuePair<string, object?> kv in value.Properties)
        {
            activity.ChannelData.Properties[kv.Key] = kv.Value;
        }

        return activity;
    }

    /// <summary>
    /// Sets a channel data key/value property.
    /// </summary>
    public static TeamsActivity WithData(this TeamsActivity activity, string key, object? value)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        activity.ChannelData ??= new TeamsChannelData();
        activity.ChannelData.Properties[key] = value;
        return activity;
    }

    /// <summary>
    /// Sets the app id inside channel data.
    /// </summary>
    public static TeamsActivity WithAppId(this TeamsActivity activity, string value)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        activity.ChannelData ??= new TeamsChannelData();
        activity.ChannelData.Properties["app"] = new Dictionary<string, object?> { ["id"] = value };
        return activity;
    }

    /// <summary>
    /// Adds one or more entities to the activity.
    /// </summary>
    /// <param name="activity">The target activity.</param>
    /// <param name="entities">Entities to add.</param>
    /// <returns>The activity for chaining.</returns>
    public static TeamsActivity AddEntity(this TeamsActivity activity, params Entity[] entities)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(entities);

        activity.Entities ??= [];
        foreach (Entity entity in entities)
        {
            activity.Entities.Add(entity);
        }

        return activity;
    }

    /// <summary>
    /// Replaces an existing entity with a new entity.
    /// </summary>
    /// <param name="activity">The target activity.</param>
    /// <param name="oldEntity">The entity to replace.</param>
    /// <param name="newEntity">The replacement entity.</param>
    /// <returns>The activity for chaining.</returns>
    public static TeamsActivity UpdateEntity(this TeamsActivity activity, Entity oldEntity, Entity newEntity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(oldEntity);
        ArgumentNullException.ThrowIfNull(newEntity);

        if (activity.Entities != null)
        {
            activity.Entities.Remove(oldEntity);
        }
        else
        {
            activity.Entities = [];
        }

        activity.Entities.Add(newEntity);
        return activity;
    }

    /// <summary>
    /// Adds the AI-generated content label to the root message entity.
    /// </summary>
    public static OMessageEntity AddAIGenerated(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        OMessageEntity messageEntity = GetOrCreateRootMessageEntity(activity);
        messageEntity.AdditionalType ??= [];

        if (!messageEntity.AdditionalType.Contains("AIGeneratedContent"))
        {
            messageEntity.AdditionalType.Add("AIGeneratedContent");
        }

        return messageEntity;
    }

    /// <summary>
    /// Enables/disables feedback loop on the activity.
    /// </summary>
    public static TeamsActivity AddFeedback(this TeamsActivity activity, bool value = true)
    {
        ArgumentNullException.ThrowIfNull(activity);

        activity.ChannelData ??= new TeamsChannelData();
        activity.ChannelData.FeedbackLoopEnabled = value;
        return activity;
    }

    /// <summary>
    /// Adds targeted message info entity for prompt preview and strips quote placeholders.
    /// </summary>
    [Experimental("ExperimentalTeamsTargeted")]
    public static T AddTargetedMessageInfo<T>(this T activity, string messageId) where T : TeamsActivity
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        if (activity.Entities is not null)
        {
            for (int i = activity.Entities.Count - 1; i >= 0; i--)
            {
                if (activity.Entities[i].Type == "quotedReply")
                {
                    activity.Entities.RemoveAt(i);
                }
            }
        }

        if (activity is MessageActivity msg && msg.Text is not null)
        {
            msg.Text = QuotedReplyEntity.QuotedPlaceholderRegex().Replace(msg.Text, string.Empty).Trim();
        }
        else if (activity.Properties.TryGetValue("text", out object? rawText) && rawText is string text)
        {
            activity.Properties["text"] = QuotedReplyEntity.QuotedPlaceholderRegex().Replace(text, string.Empty).Trim();
        }

        bool hasEntity = activity.Entities?.Any(e => e.Type == "targetedMessageInfo") ?? false;
        if (!hasEntity)
        {
            activity.AddEntity(new TargetedMessageInfoEntity { MessageId = messageId });
        }

        return activity;
    }

    /// <summary>
    /// Adds a citation claim to the activity.
    /// </summary>
    public static CitationEntity AddCitation(this TeamsActivity activity, int position, CitationAppearance appearance)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(appearance);

        activity.Entities ??= [];
        OMessageEntity messageEntity = GetOrCreateRootMessageEntity(activity);
        CitationEntity citationEntity = new(messageEntity);
        citationEntity.Citation ??= [];
        citationEntity.Citation.Add(new CitationClaim()
        {
            Position = position,
            Appearance = appearance.ToDocument()
        });

        activity.Entities.Remove(messageEntity);
        activity.Entities.Add(citationEntity);
        return citationEntity;
    }

    /// <summary>
    /// Adds a mention (@ mention) of a user or bot to the activity.
    /// </summary>
    public static MentionEntity AddMention(this TeamsActivity activity, ConversationAccount account, string? text = null, bool addText = true)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(account);

        string? mentionText = text ?? account.Name;
        if (addText && activity is MessageActivity msg)
        {
            msg.Text = $"<at>{mentionText}</at> {msg.Text}";
        }

        activity.Entities ??= [];
        MentionEntity mentionEntity = new(account, $"<at>{mentionText}</at>");
        activity.Entities.Add(mentionEntity);
        return mentionEntity;
    }

    /// <summary>
    /// Adds a content sensitivity label to the activity.
    /// </summary>
    public static TeamsActivity AddSensitivityLabel(this TeamsActivity activity, string name, string? description = null, DefinedTerm? pattern = null)
    {
        ArgumentNullException.ThrowIfNull(activity);
        activity.AddEntity(new SensitiveUsageEntity()
        {
            Name = name,
            Description = description,
            Pattern = pattern
        });
        return activity;
    }

    /// <summary>
    /// Adds client information entity to the activity.
    /// </summary>
    public static ClientInfoEntity AddClientInfo(this TeamsActivity activity, string platform, string country, string timeZone, string locale)
    {
        ArgumentNullException.ThrowIfNull(activity);

        ClientInfoEntity clientInfo = new(platform, country, timeZone, locale);
        activity.Entities ??= [];
        activity.Entities.Add(clientInfo);
        return clientInfo;
    }

    private static OMessageEntity GetOrCreateRootMessageEntity(TeamsActivity activity)
    {
        activity.Entities ??= [];

        OMessageEntity? messageEntity = activity.Entities.FirstOrDefault(
            e => e.Type == "https://schema.org/Message" && e.OType == "Message"
        ) as OMessageEntity;

        if (messageEntity is null)
        {
            messageEntity = new OMessageEntity();
            activity.Entities.Add(messageEntity);
        }

        return messageEntity;
    }
}
