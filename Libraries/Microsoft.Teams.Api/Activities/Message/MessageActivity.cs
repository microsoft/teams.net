// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Api.Entities;
using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities;

public partial class ActivityType : StringEnum
{
    public static readonly ActivityType Message = new("message");
    public bool IsMessage => Message.Equals(Value);
}

public class MessageActivity : Activity
{
    [JsonPropertyName("text")]
    [JsonPropertyOrder(31)]
    public string Text { get; set; }

    [JsonPropertyName("speak")]
    [JsonPropertyOrder(32)]
    public string? Speak { get; set; }

    [JsonPropertyName("inputHint")]
    [JsonPropertyOrder(33)]
    public InputHint? InputHint { get; set; }

    [JsonPropertyName("summary")]
    [JsonPropertyOrder(34)]
    public string? Summary { get; set; }

    [JsonPropertyName("textFormat")]
    [JsonPropertyOrder(35)]
    public TextFormat? TextFormat { get; set; }

    [JsonPropertyName("attachmentLayout")]
    [JsonPropertyOrder(121)]
    public Attachment.Layout? AttachmentLayout { get; set; }

    [JsonPropertyName("attachments")]
    [JsonPropertyOrder(122)]
    public IList<Attachment>? Attachments { get; set; }

    [JsonPropertyName("suggestedActions")]
    [JsonPropertyOrder(123)]
    public SuggestedActions? SuggestedActions { get; set; }

    [JsonPropertyName("importance")]
    [JsonPropertyOrder(39)]
    public Importance? Importance { get; set; }

    [JsonPropertyName("deliveryMode")]
    [JsonPropertyOrder(41)]
    public DeliveryMode? DeliveryMode { get; set; }

    [JsonPropertyName("expiration")]
    [JsonPropertyOrder(42)]
    public DateTime? Expiration { get; set; }

    [JsonPropertyName("value")]
    [JsonPropertyOrder(43)]
    public object? Value { get; set; }

    /// <summary>
    /// Indicates if this is a targeted message visible only to a specific recipient.
    /// </summary>
    [JsonPropertyName("isTargeted")]
    [JsonPropertyOrder(44)]
    public bool? IsTargeted { get; set; }

    [JsonIgnore]
    public bool IsRecipientMentioned
    {
        get => (Entities ?? []).Any(e => e is MentionEntity mention && mention.Mentioned.Id == Recipient.Id);
    }

    public MessageActivity() : base(ActivityType.Message)
    {
        Text ??= string.Empty;
    }

    public MessageActivity(string text) : base(ActivityType.Message)
    {
        Text = text;
    }

    public MessageActivity WithText(string text)
    {
        Text = text;
        return this;
    }

    public MessageActivity WithSpeak(string speak)
    {
        Speak = speak;
        return this;
    }

    public MessageActivity WithInputHint(InputHint inputHint)
    {
        InputHint = inputHint;
        return this;
    }

    public MessageActivity WithSummary(string summary)
    {
        Summary = summary;
        return this;
    }

    public MessageActivity WithTextFormat(TextFormat textFormat)
    {
        TextFormat = textFormat;
        return this;
    }

    public MessageActivity WithAttachmentLayout(Attachment.Layout attachmentLayout)
    {
        AttachmentLayout = attachmentLayout;
        return this;
    }

    public MessageActivity WithSuggestedActions(SuggestedActions suggestedActions)
    {
        SuggestedActions = suggestedActions;
        return this;
    }

    public MessageActivity WithImportance(Importance importance)
    {
        Importance = importance;
        return this;
    }

    public MessageActivity WithDeliveryMode(DeliveryMode deliveryMode)
    {
        DeliveryMode = deliveryMode;
        return this;
    }

    public MessageActivity WithExpiration(DateTime expiration)
    {
        Expiration = expiration;
        return this;
    }

    public MessageActivity AddText(string text)
    {
        Text += text;
        return this;
    }

    public MessageActivity AddAttachment(params Attachment[] value)
    {
        Attachments ??= [];

        foreach (var attachment in value)
        {
            Attachments.Add(attachment);
        }

        return this;
    }

    public MessageActivity AddAttachment(Teams.Cards.AdaptiveCard card)
    {
        return AddAttachment(new Attachment(card));
    }

    public MessageActivity AddAttachment(Cards.OAuthCard card)
    {
        return AddAttachment(new Attachment(card));
    }

    public MessageActivity AddAttachment(Cards.SignInCard card)
    {
        return AddAttachment(new Attachment(card));
    }

    public MessageActivity AddMention(Account account, string? text = null, bool addText = true)
    {
        var mentionText = text ?? account.Name;

        if (addText)
        {
            Text = $"<at>{mentionText}</at> {Text}";
        }

        AddEntity(new MentionEntity()
        {
            Mentioned = account,
            Text = $"<at>{mentionText}</at>"
        });

        return this;
    }

    public MessageActivity AddStreamFinal()
    {
        ChannelData ??= new();
        ChannelData.StreamId ??= Id;
        ChannelData.StreamType ??= StreamType.Final;

        AddEntity(new StreamInfoEntity()
        {
            StreamId = Id,
            StreamType = StreamType.Final
        });

        return this;
    }

    public MentionEntity? GetAccountMention(string accountId)
    {
        return (MentionEntity?)(Entities ?? []).FirstOrDefault(e => e is MentionEntity mention && mention.Mentioned.Id == accountId);
    }

    /// <summary>
    /// Mark this message as a targeted message visible only to a specific recipient.
    /// </summary>
    /// <param name="isTargeted">If true, marks this as a targeted message. The recipient will be inferred from the incoming activity context.</param>
    /// <returns>This instance for chaining</returns>
    /// <remarks>
    /// When using true, this must be sent within an activity context (not proactively).
    /// For proactive sends, use the overload that accepts an explicit recipient ID.
    /// </remarks>
    public MessageActivity WithTargetedRecipient(bool isTargeted)
    {
        IsTargeted = isTargeted;
        return this;
    }

    /// <summary>
    /// Mark this message as a targeted message visible only to a specific recipient.
    /// </summary>
    /// <param name="recipientId">The explicit recipient ID.</param>
    /// <returns>This instance for chaining</returns>
    public MessageActivity WithTargetedRecipient(string recipientId)
    {
        IsTargeted = true;
        Recipient = new Account { Id = recipientId, Name = string.Empty, Role = Role.User };
        return this;
    }

    public MessageActivity Merge(MessageActivity from)
    {
        base.Merge(from);

        Text ??= from.Text;
        Speak ??= from.Speak;
        InputHint ??= from.InputHint;
        Summary ??= from.Summary;
        TextFormat ??= from.TextFormat;
        AttachmentLayout ??= from.AttachmentLayout;
        SuggestedActions ??= from.SuggestedActions;
        Importance ??= from.Importance;
        DeliveryMode ??= from.DeliveryMode;
        Expiration ??= from.Expiration;
        Value ??= from.Value;
        AddAttachment(from.Attachments?.ToArray() ?? []);

        return this;
    }
}