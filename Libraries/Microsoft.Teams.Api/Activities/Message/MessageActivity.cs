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
    public dynamic? Value { get; set; }

    public MessageActivity() : base(ActivityType.Message)
    {
        Text = string.Empty;
    }

    public MessageActivity(string text) : base(ActivityType.Message)
    {
        Text = text;
    }

    public override MessageActivity WithId(string value) => (MessageActivity)base.WithId(value);
    public override MessageActivity WithReplyToId(string value) => (MessageActivity)base.WithReplyToId(value);
    public override MessageActivity WithChannelId(ChannelId value) => (MessageActivity)base.WithChannelId(value);
    public override MessageActivity WithFrom(Account value) => (MessageActivity)base.WithFrom(value);
    public override MessageActivity WithConversation(Conversation value) => (MessageActivity)base.WithConversation(value);
    public override MessageActivity WithRelatesTo(ConversationReference value) => (MessageActivity)base.WithRelatesTo(value);
    public override MessageActivity WithRecipient(Account value) => (MessageActivity)base.WithRecipient(value);
    public override MessageActivity WithServiceUrl(string value) => (MessageActivity)base.WithServiceUrl(value);
    public override MessageActivity WithTimestamp(DateTime value) => (MessageActivity)base.WithTimestamp(value);
    public override MessageActivity WithLocale(string value) => (MessageActivity)base.WithLocale(value);
    public override MessageActivity WithLocalTimestamp(DateTime value) => (MessageActivity)base.WithLocalTimestamp(value);
    public override MessageActivity WithData(ChannelData value) => (MessageActivity)base.WithData(value);
    public override MessageActivity WithData(string key, object? value) => (MessageActivity)base.WithData(key, value);
    public override MessageActivity WithAppId(string value) => (MessageActivity)base.WithAppId(value);
    public override MessageActivity AddEntity(params IEntity[] entities) => (MessageActivity)base.AddEntity(entities);
    public override MessageActivity AddAIGenerated() => (MessageActivity)base.AddAIGenerated();
    public override MessageActivity AddSensitivityLabel(string name, string? description = null, DefinedTerm? pattern = null) => (MessageActivity)base.AddSensitivityLabel(name, description, pattern);
    public override MessageActivity AddFeedback(bool value = true) => (MessageActivity)base.AddFeedback(value);
    public override MessageActivity AddCitation(int position, CitationAppearance appearance) => (MessageActivity)base.AddCitation(position, appearance);

    public MessageActivity AddAttachment(params Attachment[] value)
    {
        Attachments ??= [];

        foreach (var attachment in value)
        {
            Attachments.Add(attachment);
        }

        return this;
    }

    public MessageActivity AddAttachment(Teams.Cards.Card card)
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

    public MessageActivity AddMention(Account account)
    {
        return AddEntity(new MentionEntity()
        {
            Mentioned = account,
            Text = $"<at>{account.Name}</at>"
        });
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

    public bool IsRecipientMentioned()
    {
        return (Entities ?? []).Any(e => e is MentionEntity mention && mention.Mentioned.Id == Recipient.Id);
    }

    public MentionEntity? GetAccountMention(string accountId)
    {
        return (MentionEntity?)(Entities ?? []).FirstOrDefault(e => e is MentionEntity mention && mention.Mentioned.Id == accountId);
    }
}