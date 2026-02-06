// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a message activity.
/// </summary>
public class MessageActivity : Activity
{
    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the SSML speak content of the message.
    /// </summary>
    [JsonPropertyName("speak")]
    public string? Speak { get; set; }

    /// <summary>
    /// Gets or sets the input hint. See <see cref="InputHints"/> for common values.
    /// </summary>
    [JsonPropertyName("inputHint")]
    public string? InputHint { get; set; }

    /// <summary>
    /// Gets or sets the summary of the message.
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the text format. See <see cref="TextFormats"/> for common values.
    /// </summary>
    [JsonPropertyName("textFormat")]
    public string? TextFormat { get; set; }

    /// <summary>
    /// Gets or sets the attachment layout.
    /// </summary>
    [JsonPropertyName("attachmentLayout")]
    public string? AttachmentLayout { get; set; }

    /// <summary>
    /// Gets or sets the importance. See <see cref="ImportanceLevels"/> for common values.
    /// </summary>
    [JsonPropertyName("importance")]
    public string? Importance { get; set; }

    /// <summary>
    /// Gets or sets the delivery mode. See <see cref="DeliveryModes"/> for common values.
    /// </summary>
    [JsonPropertyName("deliveryMode")]
    public string? DeliveryMode { get; set; }

    /// <summary>
    /// Gets or sets the expiration time of the message.
    /// </summary>
    [JsonPropertyName("expiration")]
    public DateTime? Expiration { get; set; }

    /// <summary>
    /// Gets or sets the value associated with the message.
    /// </summary>
    [JsonPropertyName("value")]
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
    public MessageActivity(string text) : base(ActivityTypes.Message)
    {
        Text = text;
    }
}

/// <summary>
/// String constants for input hints.
/// </summary>
public static class InputHints
{
    /// <summary>
    /// Accepting input hint.
    /// </summary>
    public const string AcceptingInput = "acceptingInput";

    /// <summary>
    /// Ignoring input hint.
    /// </summary>
    public const string IgnoringInput = "ignoringInput";

    /// <summary>
    /// Expecting input hint.
    /// </summary>
    public const string ExpectingInput = "expectingInput";
}

/// <summary>
/// String constants for text formats.
/// </summary>
public static class TextFormats
{
    /// <summary>
    /// Plain text format.
    /// </summary>
    public const string Plain = "plain";

    /// <summary>
    /// Markdown text format.
    /// </summary>
    public const string Markdown = "markdown";

    /// <summary>
    /// XML text format.
    /// </summary>
    public const string Xml = "xml";
}

/// <summary>
/// String constants for importance levels.
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

    /// <summary>
    /// Urgent importance.
    /// </summary>
    public const string Urgent = "urgent";
}

/// <summary>
/// String constants for delivery modes.
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
    /// Ephemeral delivery mode.
    /// </summary>
    public const string Ephemeral = "ephemeral";

    /// <summary>
    /// Expected replies delivery mode.
    /// </summary>
    public const string ExpectedReplies = "expectReplies";
}
