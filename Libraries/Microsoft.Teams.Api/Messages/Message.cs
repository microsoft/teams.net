using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Messages;

/// <summary>
/// Represents the individual message within a chat or channel where a message
/// actions is taken.
/// </summary>
public class Message
{
    /// <summary>
    /// Unique id of the message.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    /// <summary>
    /// Id of the parent/root message of the thread.
    /// </summary>
    [JsonPropertyName("replyToId")]
    [JsonPropertyOrder(1)]
    public string? ReplyToId { get; set; }

    /// <summary>
    /// Type of message - automatically set to
    /// message. Possible values include: 'message'
    /// </summary>
    [JsonPropertyName("messageType")]
    [JsonPropertyOrder(2)]
    public string? MessageType { get; set; } = "message";

    /// <summary>
    /// Timestamp of when the message was created.
    /// </summary>
    [JsonPropertyName("createdDateTime")]
    [JsonPropertyOrder(3)]
    public string? CreatedDateTime { get; set; }

    /// <summary>
    /// Timestamp of when the message was edited or updated.
    /// </summary>
    [JsonPropertyName("lastModifiedDateTime")]
    [JsonPropertyOrder(4)]
    public string? LastModifiedDateTime { get; set; }

    /// <summary>
    /// Indicates whether a message has been soft deleted.
    /// </summary>
    [JsonPropertyName("deleted")]
    [JsonPropertyOrder(5)]
    public bool? Deleted { get; set; }

    /// <summary>
    /// Subject line of the message.
    /// </summary>
    [JsonPropertyName("subject")]
    [JsonPropertyOrder(6)]
    public string? Subject { get; set; }

    /// <summary>
    /// Summary text of the message that could be used for notifications.
    /// </summary>
    [JsonPropertyName("summary")]
    [JsonPropertyOrder(7)]
    public string? Summary { get; set; }

    /// <summary>
    /// The importance of the message. Possible
    /// values include: 'normal', 'high', 'urgent'
    /// </summary>
    [JsonPropertyName("importance")]
    [JsonPropertyOrder(8)]
    public Importance? Importance { get; set; }

    /// <summary>
    /// Locale of the message set by the client.
    /// </summary>
    [JsonPropertyName("locale")]
    [JsonPropertyOrder(9)]
    public string? Locale { get; set; }

    /// <summary>
    /// Link back to the message.
    /// </summary>
    [JsonPropertyName("linkToMessage")]
    [JsonPropertyOrder(10)]
    public string? LinkToMessage { get; set; }

    /// <summary>
    /// Sender of the message.
    /// </summary>
    [JsonPropertyName("from")]
    [JsonPropertyOrder(11)]
    public From? From { get; set; }

    /// <summary>
    /// Plaintext/HTML representation of the content of the message.
    /// </summary>
    [JsonPropertyName("body")]
    [JsonPropertyOrder(12)]
    public Body? Body { get; set; }

    /// <summary>
    /// How the attachment(s) are displayed in the message.
    /// </summary>
    [JsonPropertyName("attachmentLayout")]
    [JsonPropertyOrder(13)]
    public Attachment.Layout? AttachmentLayout { get; set; }

    /// <summary>
    /// Attachments in the message - card, image, file, etc.
    /// </summary>
    [JsonPropertyName("attachments")]
    [JsonPropertyOrder(14)]
    public IList<Attachment>? Attachments { get; set; }

    /// <summary>
    /// List of entities mentioned in the message.
    /// </summary>
    [JsonPropertyName("mentions")]
    [JsonPropertyOrder(15)]
    public IList<Mention>? Mentions { get; set; }

    /// <summary>
    /// Reactions for the message.
    /// </summary>
    [JsonPropertyName("reactions")]
    [JsonPropertyOrder(16)]
    public IList<Reaction>? Reactions { get; set; }
}