// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Message text format constants.
/// </summary>
public static class MessageTextFormats
{
    /// <summary>
    /// Markdown text format.
    /// </summary>
    public const string Markdown = "markdown";

    /// <summary>
    /// Plain text format.
    /// </summary>
    public const string Plain = "plain";

    /// <summary>
    /// XML text format.
    /// </summary>
    public const string Xml = "xml";
}

/// <summary>
/// Input hint constants.
/// </summary>
public static class InputHints
{
    /// <summary>
    /// The bot is accepting input.
    /// </summary>
    public const string AcceptingInput = "acceptingInput";

    /// <summary>
    /// The bot is ignoring input.
    /// </summary>
    public const string IgnoringInput = "ignoringInput";

    /// <summary>
    /// The bot is expecting input.
    /// </summary>
    public const string ExpectingInput = "expectingInput";
}

/// <summary>
/// Message importance constants.
/// </summary>
public static class ImportanceLevels
{
    /// <summary>
    /// Low importance.
    /// </summary>
    public const string Low = "low";

    /// <summary>
    /// Normal importance.
    /// </summary>
    public const string Normal = "normal";

    /// <summary>
    /// High importance.
    /// </summary>
    public const string High = "high";
}

/// <summary>
/// Delivery mode constants.
/// </summary>
public static class DeliveryModes
{
    /// <summary>
    /// Normal delivery mode.
    /// </summary>
    public const string Normal = "normal";

    /// <summary>
    /// Notification delivery mode.
    /// </summary>
    public const string Notification = "notification";

    /// <summary>
    /// Expect replies delivery mode.
    /// </summary>
    public const string ExpectReplies = "expectReplies";

    /// <summary>
    /// Ephemeral delivery mode.
    /// </summary>
    public const string Ephemeral = "ephemeral";
}

/// <summary>
/// Attachment layout constants.
/// </summary>
public static class AttachmentLayouts
{
    /// <summary>
    /// List attachment layout.
    /// </summary>
    public const string List = "list";

    /// <summary>
    /// Carousel attachment layout.
    /// </summary>
    public const string Carousel = "carousel";
}

/// <summary>
/// Content type constants.
/// </summary>
public static class ContentTypes
{
    /// <summary>
    /// HTML content type.
    /// </summary>
    public const string Html = "text/html";

    /// <summary>
    /// Text content type.
    /// </summary>
    public const string Text = "text";

    /// <summary>
    /// Adaptive card content type.
    /// </summary>
    public const string AdaptiveCard = "application/vnd.microsoft.card.adaptive";

    /// <summary>
    /// Animation card content type.
    /// </summary>
    public const string AnimationCard = "application/vnd.microsoft.card.animation";

    /// <summary>
    /// Audio card content type.
    /// </summary>
    public const string AudioCard = "application/vnd.microsoft.card.audio";

    /// <summary>
    /// Hero card content type.
    /// </summary>
    public const string HeroCard = "application/vnd.microsoft.card.hero";

    /// <summary>
    /// OAuth card content type.
    /// </summary>
    public const string OAuthCard = "application/vnd.microsoft.card.oauth";

    /// <summary>
    /// Sign-in card content type.
    /// </summary>
    public const string SignInCard = "application/vnd.microsoft.card.signin";

    /// <summary>
    /// Thumbnail card content type.
    /// </summary>
    public const string ThumbnailCard = "application/vnd.microsoft.card.thumbnail";

    /// <summary>
    /// Video card content type.
    /// </summary>
    public const string VideoCard = "application/vnd.microsoft.card.video";
}

/// <summary>
/// Represents an attachment to an activity.
/// </summary>
public class Attachment
{
    /// <summary>
    /// Gets or sets the identifier of the attachment.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the attachment.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the MIME type/content type for the attachment.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the content URL.
    /// </summary>
    public Uri? ContentUrl { get; set; }

    /// <summary>
    /// Gets or sets the embedded content.
    /// </summary>
    public object? Content { get; set; }

    /// <summary>
    /// Gets or sets the thumbnail URL associated with the attachment.
    /// </summary>
    public Uri? ThumbnailUrl { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Attachment"/> class.
    /// </summary>
    public Attachment()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Attachment"/> class with the specified content.
    /// </summary>
    /// <param name="content">The embedded content.</param>
    public Attachment(object? content)
    {
        Content = content;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Attachment"/> class with the specified content type and content.
    /// </summary>
    /// <param name="contentType">The content type.</param>
    /// <param name="content">The embedded content.</param>
    public Attachment(string? contentType, object? content = null)
    {
        ContentType = contentType;
        Content = content;
    }
}

/// <summary>
/// Represents an action in a suggested actions or card.
/// </summary>
public class CardAction
{
    /// <summary>
    /// Gets or sets the type of action.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the title of the action.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the image URL for the action.
    /// </summary>
    public Uri? Image { get; set; }

    /// <summary>
    /// Gets or sets the text of the action.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the display text of the action.
    /// </summary>
    public string? DisplayText { get; set; }

    /// <summary>
    /// Gets or sets the value associated with the action.
    /// </summary>
    public object? Value { get; set; }
}

/// <summary>
/// Represents suggested actions for an activity.
/// </summary>
public class SuggestedActions
{
    /// <summary>
    /// Gets or sets the identifiers of the recipients that the actions should be shown to.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Matches source API structure for serialization")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Matches source API structure")]
    public List<string>? To { get; set; }

    /// <summary>
    /// Gets or sets the actions that can be shown to the user.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Matches source API structure for serialization")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Matches source API structure")]
    public List<CardAction>? Actions { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SuggestedActions"/> class.
    /// </summary>
    public SuggestedActions()
    {
    }
}

/// <summary>
/// Represents a message activity.
/// </summary>
public class MessageActivity : Activity
{
    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the SSML (Speech Synthesis Markup Language) content.
    /// </summary>
    public string? Speak { get; set; }

    /// <summary>
    /// Gets or sets the input hint (e.g., "acceptingInput", "ignoringInput", "expectingInput").
    /// </summary>
    public string? InputHint { get; set; }

    /// <summary>
    /// Gets or sets the summary of the message.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the text format (e.g., "markdown", "plain", "xml").
    /// </summary>
    public string? TextFormat { get; set; }

    /// <summary>
    /// Gets or sets the attachment layout (e.g., "list", "carousel").
    /// </summary>
    public string? AttachmentLayout { get; set; }

    /// <summary>
    /// Gets or sets the list of attachments.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Matches source API structure for serialization")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Matches source API structure")]
    public List<Attachment>? Attachments { get; set; }

    /// <summary>
    /// Gets or sets the suggested actions.
    /// </summary>
    public SuggestedActions? SuggestedActions { get; set; }

    /// <summary>
    /// Gets or sets the importance level (e.g., "low", "normal", "high").
    /// </summary>
    public string? Importance { get; set; }

    /// <summary>
    /// Gets or sets the delivery mode (e.g., "normal", "notification", "expectReplies", "ephemeral").
    /// </summary>
    public string? DeliveryMode { get; set; }

    /// <summary>
    /// Gets or sets the expiration time of the message.
    /// </summary>
    public DateTime? Expiration { get; set; }

    /// <summary>
    /// Gets or sets a value object for the message.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageActivity"/> class.
    /// </summary>
    public MessageActivity() : base(ActivityTypes.Message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageActivity"/> class with the specified text.
    /// </summary>
    /// <param name="text">The text content of the message.</param>
    public MessageActivity(string? text) : base(ActivityTypes.Message)
    {
        Text = text;
    }
}
