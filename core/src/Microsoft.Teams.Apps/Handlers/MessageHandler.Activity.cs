// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Apps.Utils;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Represents a message activity.
/// </summary>
public class MessageActivity : TeamsActivity
{

    /// <summary>
    /// Convenience method to create a MessageActivity from a CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    /// <returns>A MessageActivity instance.</returns>
    public static new MessageActivity FromActivity(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return new MessageActivity(activity);
    }

    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    [Obsolete("MessageActivity is an inbound (received) activity. To construct and send a message, use new MessageActivityInput() instead.")]
    public MessageActivity() : base(TeamsActivityTypes.Message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageActivity"/> class with the specified text.
    /// </summary>
    /// <param name="text">The text content of the message.</param>
    [Obsolete("MessageActivity is an inbound (received) activity. To construct and send a message, use new MessageActivityInput() instead.")]
    public MessageActivity(string text) : base(TeamsActivityTypes.Message)
    {
        Text = text;
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="MessageActivity"/> class with the specified text.
    /// </summary>
    /// <param name="attachments">The list of attachments for the message.</param>
    [Obsolete("MessageActivity is an inbound (received) activity. To construct and send a message, use new MessageActivityInput() instead.")]
    public MessageActivity(IList<TeamsAttachment> attachments) : base(TeamsActivityTypes.Message)
    {
        Attachments = attachments;
    }

    /// <summary>
    /// Internal constructor to create MessageActivity from CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    internal MessageActivity(CoreActivity activity) : base(activity)
    {
        Attachments = Properties.Extract<IList<TeamsAttachment>>("attachments");
        Text = Properties.Extract<string>("text");
        TextFormat = Properties.Extract<TextFormat>("textFormat");
        AttachmentLayout = Properties.Extract<AttachmentLayoutType>("attachmentLayout");
        SuggestedActions = Properties.Extract<SuggestedActions>("suggestedActions");
    }

    /// <summary>
    /// Gets the attachments for the message.
    /// </summary>
    [JsonPropertyName("attachments")]
    public IList<TeamsAttachment>? Attachments { get; internal set; }

    /// <summary>
    /// Gets the text content of the message.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; internal set; }

    /// <summary>
    /// Gets the message text with the bot (recipient) @mention removed and trimmed.
    /// In group chats, Teams prepends "&lt;at&gt;botname&lt;/at&gt;" to the text when the bot is mentioned.
    /// This property strips that mention so handlers can match on the user's intent alone.
    /// </summary>
    [JsonIgnore]
    public string? TextWithoutMentions
    {
        get
        {
            string? text = Text;
            if (text is null) return null;

            foreach (MentionEntity mention in this.GetMentions())
            {
                if (mention.Mentioned?.Id == Recipient?.Id && mention.Text is not null)
                {
                    text = text.Replace(mention.Text, string.Empty, StringComparison.OrdinalIgnoreCase);
                }
            }
            return text.Trim();
        }
    }
    /// <summary>
    /// Gets the text format. See <see cref="TextFormats"/> for common values (plain, markdown, xml, extendedmarkdown).
    /// </summary>
    [JsonPropertyName("textFormat")]
    public TextFormat? TextFormat { get; internal set; }

    /// <summary>
    /// Gets the attachment layout.
    /// </summary>
    [JsonPropertyName("attachmentLayout")]
    public AttachmentLayoutType? AttachmentLayout { get; internal set; }

}

/// <summary>
/// String constants for text formats.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<TextFormat>))]
public class TextFormat(string value) : StringEnum(value)
{
    /// <summary>
    /// Plain text format.
    /// </summary>
    public static readonly TextFormat Plain = new("plain");

    /// <summary>
    /// Markdown text format.
    /// </summary>
    public static readonly TextFormat Markdown = new("markdown");

    /// <summary>
    /// XML text format.
    /// </summary>
    public static readonly TextFormat Xml = new("xml");

    /// <summary>
    /// Extended markdown text format. Supports GFM tables, LaTeX math blocks,
    /// and other rich content beyond standard markdown.
    /// </summary>
    /// <remarks>
    /// This format is currently in public preview and may be subject to change.
    /// </remarks>
    [Experimental("ExperimentalTeamsExtendedMarkdown")]
    public static readonly TextFormat ExtendedMarkdown = new("extendedmarkdown");
}

/// <summary>
/// String constants for text formats.
/// </summary>
public static class TextFormats
{
    /// <summary>
    /// Plain text format.
    /// </summary>
    public static TextFormat Plain => TextFormat.Plain;

    /// <summary>
    /// Markdown text format.
    /// </summary>
    public static TextFormat Markdown => TextFormat.Markdown;

    /// <summary>
    /// XML text format.
    /// </summary>
    public static TextFormat Xml => TextFormat.Xml;

    /// <summary>
    /// Extended markdown text format. Supports GFM tables, LaTeX math blocks,
    /// and other rich content beyond standard markdown.
    /// </summary>
    /// <remarks>
    /// This format is currently in public preview and may be subject to change.
    /// </remarks>
    [Experimental("ExperimentalTeamsExtendedMarkdown")]
    public static TextFormat ExtendedMarkdown => TextFormat.ExtendedMarkdown;
}

/// <summary>
/// Fluent extension methods for <see cref="MessageActivity"/>.
/// <para>
/// These are retained only for backward compatibility. <see cref="MessageActivity"/> is an inbound
/// (received) activity type. To construct and send a message, use
/// <see cref="MessageActivityInput"/> with fluent methods instead.
/// </para>
/// </summary>
public static class MessageActivityExtensions
{
    private const string ObsoleteMessage =
        "MessageActivity is an inbound (received) activity. To construct and send a message, use new MessageActivityInput() instead.";

    private static readonly Regex QuotedPlaceholderRegex = new("<quoted messageId=\"[^\"]*\"/>", RegexOptions.Compiled);

    /// <summary>
    /// Sets the activity id.
    /// </summary>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity WithId(this MessageActivity message, string value)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Id = value;
        return message;
    }

    /// <summary>
    /// Sets the channel id.
    /// </summary>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity WithChannelId(this MessageActivity message, string? value)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.ChannelId = value;
        return message;
    }

    /// <summary>
    /// Sets the sender account.
    /// </summary>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity WithFrom(this MessageActivity message, ChannelAccount? value)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.From = value is TeamsChannelAccount teamsAccount
            ? teamsAccount
            : TeamsChannelAccount.FromChannelAccount(value);
        return message;
    }

    /// <summary>
    /// Sets the recipient account on the message.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="account">The recipient account.</param>
    /// <returns>The message activity for chaining.</returns>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity WithRecipient(this MessageActivity message, ChannelAccount account)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.Recipient = account is TeamsChannelAccount teamsAccount
            ? teamsAccount
            : TeamsChannelAccount.FromChannelAccount(account);
        return message;
    }

    /// <summary>
    /// Sets the recipient account and targeted flag on the message.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="account">The recipient account.</param>
    /// <param name="isTargeted">Whether the recipient is targeted.</param>
    /// <returns>The message activity for chaining.</returns>
    [Obsolete(ObsoleteMessage)]
    [Experimental("ExperimentalTeamsTargeted")]
    public static MessageActivity WithRecipient(this MessageActivity message, ChannelAccount account, bool isTargeted = false)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (account is not null)
        {
            account.IsTargeted = isTargeted ? true : null;
            message.Recipient = account is TeamsChannelAccount teamsAccount
                ? teamsAccount
                : TeamsChannelAccount.FromChannelAccount(account);
        }
        return message;
    }

    /// <summary>
    /// Sets the conversation information.
    /// </summary>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity WithConversation(this MessageActivity message, Conversation? value)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.Conversation = value is TeamsConversation teamsConversation
            ? teamsConversation
            : TeamsConversation.FromConversation(value);
        return message;
    }

    /// <summary>
    /// Sets the service url.
    /// </summary>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity WithServiceUrl(this MessageActivity message, Uri? value)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.ServiceUrl = value;
        return message;
    }

    /// <summary>
    /// Sets the locale.
    /// </summary>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity WithLocale(this MessageActivity message, string? value)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Locale = value;
        return message;
    }

    /// <summary>
    /// Sets the UTC timestamp value.
    /// </summary>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity WithTimestamp(this MessageActivity message, string? value)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Timestamp = value;
        return message;
    }

    /// <summary>
    /// Sets the local timestamp value.
    /// </summary>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity WithLocalTimestamp(this MessageActivity message, string? value)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.LocalTimestamp = value;
        return message;
    }

    /// <summary>
    /// Sets a channel data key/value property.
    /// </summary>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity WithData(this MessageActivity message, string key, object? value)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        message.ChannelData ??= new TeamsChannelData();
        message.ChannelData.Properties[key] = value;
        return message;
    }

    /// <summary>
    /// Sets the app id inside channel data.
    /// </summary>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity WithAppId(this MessageActivity message, string value)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        message.ChannelData ??= new TeamsChannelData();
        message.ChannelData.App ??= new AppInfo();
        message.ChannelData.App.Id = value;
        return message;
    }

    /// <summary>
    /// Sets the text content of the message.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="text">The text to set.</param>
    /// <returns>The message activity for chaining.</returns>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity WithText(this MessageActivity message, string text)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Text = text;
        message.TextFormat = TextFormats.Plain;
        return message;
    }

    /// <summary>
    /// Sets the text content of the message.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="text">The text to set.</param>
    /// <param name="textFormat">The text format.</param>
    /// <returns>The message activity for chaining.</returns>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity WithText(this MessageActivity message, string text, TextFormat textFormat)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Text = text;
        message.TextFormat = textFormat;
        return message;
    }

    /// <summary>
    /// Appends text to the current message text.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="text">The text to append.</param>
    /// <returns>The message activity for chaining.</returns>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity AddText(this MessageActivity message, string text)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Text = $"{message.Text}{text}";
        return message;
    }

    /// <summary>
    /// Sets the text format for the message.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="textFormat">The text format. See <see cref="TextFormats"/> for common values.</param>
    /// <returns>The message activity for chaining.</returns>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity WithTextFormat(this MessageActivity message, TextFormat textFormat)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.TextFormat = textFormat;
        return message;
    }

    /// <summary>
    /// Adds one or more attachments to the message.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="attachments">The attachments to add.</param>
    /// <returns>The message activity for chaining.</returns>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity AddAttachment(this MessageActivity message, params TeamsAttachment[] attachments)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(attachments);
        message.Attachments ??= [];
        foreach (TeamsAttachment attachment in attachments)
        {
            message.Attachments.Add(attachment);
        }
        return message;
    }

    /// <summary>
    /// Sets the attachment layout for the message.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="attachmentLayout">The attachment layout (e.g., "list", "carousel").</param>
    /// <returns>The message activity for chaining.</returns>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity WithAttachmentLayout(this MessageActivity message, AttachmentLayoutType attachmentLayout)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.AttachmentLayout = attachmentLayout;
        return message;
    }

    /// <summary>
    /// Sets the suggested actions for the message.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="suggestedActions">The suggested actions to set.</param>
    /// <returns>The message activity for chaining.</returns>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity WithSuggestedActions(this MessageActivity message, SuggestedActions suggestedActions)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.SuggestedActions = suggestedActions;
        return message;
    }

    /// <summary>
    /// Adds one or more entities to the message.
    /// </summary>
    /// <param name="message">The target message.</param>
    /// <param name="entities">Entities to add.</param>
    /// <returns>The message for chaining.</returns>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity AddEntity(this MessageActivity message, params Entity[] entities)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(entities);

        message.Entities ??= [];
        foreach (Entity entity in entities)
        {
            message.Entities.Add(entity);
        }

        return message;
    }

    /// <summary>
    /// Replaces an existing entity with a new entity.
    /// </summary>
    /// <param name="message">The target message.</param>
    /// <param name="oldEntity">The entity to replace.</param>
    /// <param name="newEntity">The replacement entity.</param>
    /// <returns>The message for chaining.</returns>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity UpdateEntity(this MessageActivity message, Entity oldEntity, Entity newEntity)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(oldEntity);
        ArgumentNullException.ThrowIfNull(newEntity);

        if (message.Entities != null)
        {
            message.Entities.Remove(oldEntity);
        }
        else
        {
            message.Entities = [];
        }

        message.Entities.Add(newEntity);
        return message;
    }

    /// <summary>
    /// Adds a quoted message reference and appends a placeholder to the message text.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="messageId">The ID of the message being quoted.</param>
    /// <param name="text">Optional text to append after the quote placeholder.</param>
    /// <returns>The message activity for chaining.</returns>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity AddQuote(this MessageActivity message, string messageId, string? text = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        message.Entities ??= [];
        message.Entities.Add(new QuotedReplyEntity { QuotedReply = new QuotedReplyData { MessageId = messageId } });

        string newText = (message.Text ?? string.Empty) + QuotedReplyEntityExtensions.QuotedPlaceholder(messageId);
        if (text != null)
        {
            newText += $" {text}";
        }
        message.Text = newText;

        return message;
    }

    /// <summary>
    /// Prepends a quoted message placeholder before existing text.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="messageId">The ID of the message being quoted.</param>
    /// <returns>The message activity for chaining.</returns>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity PrependQuote(this MessageActivity message, string messageId)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        message.Entities ??= [];
        message.Entities.Insert(0, new QuotedReplyEntity { QuotedReply = new QuotedReplyData { MessageId = messageId } });
        string placeholder = QuotedReplyEntityExtensions.QuotedPlaceholder(messageId);
        string text = message.Text?.Trim() ?? "";
        message.Text = string.IsNullOrEmpty(text) ? placeholder : $"{placeholder} {text}";

        return message;
    }


    /// <summary>
    /// Adds targeted message info entity for prompt preview and strips quote placeholders.
    /// </summary>
    [Obsolete(ObsoleteMessage)]
    [Experimental("ExperimentalTeamsTargeted")]
    public static MessageActivity AddTargetedMessageInfo(this MessageActivity message, string messageId)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        // Remove any existing quotedReply entities to prevent conflicts with the new targeted message info entity.
        if (message.Entities is not null)
        {
            for (int i = message.Entities.Count - 1; i >= 0; i--)
            {
                if (message.Entities[i].Type == "quotedReply")
                {
                    message.Entities.RemoveAt(i);
                }
            }
        }

        if (message.Text is not null)
        {
            message.Text = QuotedPlaceholderRegex.Replace(message.Text, string.Empty).Trim();
        }

        bool hasEntity = message.Entities?.Any(e => e.Type == "targetedMessageInfo") ?? false;
        if (!hasEntity)
        {
            message.Entities ??= [];
            message.Entities.Add(new TargetedMessageInfoEntity { MessageId = messageId });
        }

        return message;
    }

    /// <summary>
    /// Adds a mention (@mention) entity and optionally prepends mention text.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="account">The account being mentioned.</param>
    /// <param name="text">Optional mention text. If null, uses account name.</param>
    /// <param name="addText">Whether mention text should be prepended to message text.</param>
    /// <returns>The message activity for chaining.</returns>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity AddMention(this MessageActivity message, ChannelAccount account, string? text = null, bool addText = true)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(account);

        string? mentionText = text ?? account.Name;

        if (addText)
        {
            message.Text = $"<at>{mentionText}</at> {message.Text}";
        }

        message.Entities ??= [];
        message.Entities.Add(new MentionEntity(account, $"<at>{mentionText}</at>"));

        return message;
    }

    /// <summary>
    /// Marks the message as a final streaming message by adding a <see cref="StreamInfoEntity"/>
    /// with <see cref="StreamTypes.Final"/>.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <returns>The message activity for chaining.</returns>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity AddStreamFinal(this MessageActivity message)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.ChannelData ??= new TeamsChannelData();

        string? resolvedStreamId = null;
        if (message.ChannelData.Properties.TryGetValue("streamId", out object? existingStreamId) && existingStreamId is not null)
        {
            resolvedStreamId = existingStreamId.ToString();
        }
        else
        {
            resolvedStreamId = message.Id;
        }

        message.ChannelData.Properties["streamId"] = resolvedStreamId;
        message.ChannelData.Properties["streamType"] = StreamTypes.Final;

        message.Entities ??= [];
        message.Entities.Add(new StreamInfoEntity
        {
            StreamId = resolvedStreamId,
            StreamType = StreamTypes.Final,
            StreamSequence = null
        });

        return message;
    }

    /// <summary>
    /// Gets the mention entity for a specific account id.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="accountId">The account id to match.</param>
    /// <returns>The matching mention entity, or null if not found.</returns>
    [Obsolete(ObsoleteMessage)]
    public static MentionEntity? GetAccountMention(this MessageActivity message, string accountId)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        return (MentionEntity?)(message.Entities ?? []).FirstOrDefault(e => e is MentionEntity mention && mention.Mentioned?.Id == accountId);
    }

    /// <summary>
    /// Adds the AI-generated content label to the root message entity.
    /// </summary>
    [Obsolete(ObsoleteMessage)]
    public static OMessageEntity AddAIGenerated(this MessageActivity message)
    {
        ArgumentNullException.ThrowIfNull(message);

        OMessageEntity messageEntity = GetOrCreateRootMessageEntity(message);
        messageEntity.AdditionalType ??= [];
        if (!messageEntity.AdditionalType.Contains("AIGeneratedContent"))
        {
            messageEntity.AdditionalType.Add("AIGeneratedContent");
        }

        return messageEntity;
    }

    /// <summary>
    /// Adds a content sensitivity label to the message.
    /// </summary>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity AddSensitivityLabel(this MessageActivity message, string name, string? description = null, DefinedTerm? pattern = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        message.Entities ??= [];
        message.Entities.Add(new SensitiveUsageEntity
        {
            Name = name,
            Description = description,
            Pattern = pattern
        });
        return message;
    }

    /// <summary>
    /// Enables/disables feedback loop on the message.
    /// </summary>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity AddFeedback(this MessageActivity message, bool value = true)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.ChannelData ??= new TeamsChannelData();
        message.ChannelData.FeedbackLoopEnabled = value;
        return message;
    }

    /// <summary>
    /// Configures feedback loop mode on the message.
    /// </summary>
    /// <param name="message">The message activity.</param>
    /// <param name="mode">The feedback loop type. See <see cref="FeedbackTypes"/> for known values.</param>
    /// <returns>The message activity for chaining.</returns>
    [Obsolete(ObsoleteMessage)]
    public static MessageActivity AddFeedback(this MessageActivity message, string mode)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.ChannelData ??= new TeamsChannelData();
        message.ChannelData.FeedbackLoop = new FeedbackLoop(new FeedbackType(mode));
        message.ChannelData.FeedbackLoopEnabled = null;
        return message;
    }

    /// <summary>
    /// Adds a citation claim to the message.
    /// </summary>
    [Obsolete(ObsoleteMessage)]
    public static CitationEntity AddCitation(this MessageActivity message, int position, CitationAppearance appearance)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(appearance);

        message.Entities ??= [];

        OMessageEntity existingMessageEntity = GetOrCreateRootMessageEntity(message);

        // Remove existing message entity to replace with citation entity
        message.Entities.Remove(existingMessageEntity);

        CitationEntity citationEntity = new(existingMessageEntity);
        citationEntity.Citation ??= [];
        citationEntity.Citation.Add(new CitationClaim
        {
            Position = position,
            Appearance = appearance.ToDocument()
        });

        message.Entities.Add(citationEntity);
        return citationEntity;
    }

    private static OMessageEntity GetOrCreateRootMessageEntity(MessageActivity message)
    {
        message.Entities ??= [];

        OMessageEntity? messageEntity = message.Entities.FirstOrDefault(
            e => e.Type == "https://schema.org/Message" && e.OType == "Message"
        ) as OMessageEntity;

        if (messageEntity is null)
        {
            messageEntity = new OMessageEntity();
            message.Entities.Add(messageEntity);
        }

        return messageEntity;
    }
}
