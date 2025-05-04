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
        AddEntity(new MentionEntity()
        {
            Mentioned = account,
            Text = $"<at>{account.Name}</at>"
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