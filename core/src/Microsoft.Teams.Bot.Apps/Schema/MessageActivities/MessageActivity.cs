// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema.MessageActivities;

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
    /// Deserializes a JSON string into a MessageActivity instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A MessageActivity instance.</returns>
    public static new MessageActivity FromJsonString(string json)
    {
        MessageActivity activity = JsonSerializer.Deserialize(
            json, TeamsActivityJsonContext.Default.MessageActivity)!;
        activity.Rebase();
        return activity;
    }

    /// <summary>
    /// Serializes the MessageActivity to JSON with all message-specific properties.
    /// </summary>
    /// <returns>JSON string representation of the MessageActivity</returns>
    public new string ToJson()
        => ToJson(TeamsActivityJsonContext.Default.MessageActivity);

    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    public MessageActivity() : base(TeamsActivityType.Message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageActivity"/> class with the specified text.
    /// </summary>
    /// <param name="text">The text content of the message.</param>
    public MessageActivity(string text) : base(TeamsActivityType.Message)
    {
        Text = text;
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="MessageActivity"/> class with the specified text.
    /// </summary>
    /// <param name="attachments">The list of attachments for the message.</param>
    public MessageActivity(IList<TeamsAttachment> attachments) : base(TeamsActivityType.Message)
    {
        Attachments = attachments;
    }

    /// <summary>
    /// Internal constructor to create MessageActivity from CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    protected MessageActivity(CoreActivity activity) : base(activity)
    {
    }

    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text
    {
        get => base.Properties.TryGetValue("text", out var value) ? value?.ToString() : null;
        set => base.Properties["text"] = value;
    }

    /// <summary>
    /// Gets or sets the SSML speak content of the message.
    /// </summary>
    [JsonPropertyName("speak")]
    public string? Speak
    {
        get => base.Properties.TryGetValue("speak", out var value) ? value?.ToString() : null;
        set => base.Properties["speak"] = value;
    }

    /// <summary>
    /// Gets or sets the input hint. See <see cref="InputHints"/> for common values.
    /// </summary>
    [JsonPropertyName("inputHint")]
    public string? InputHint
    {
        get => base.Properties.TryGetValue("inputHint", out var value) ? value?.ToString() : null;
        set => base.Properties["inputHint"] = value;
    }

    /// <summary>
    /// Gets or sets the summary of the message.
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary
    {
        get => base.Properties.TryGetValue("summary", out var value) ? value?.ToString() : null;
        set => base.Properties["summary"] = value;
    }

    /// <summary>
    /// Gets or sets the text format. See <see cref="TextFormats"/> for common values.
    /// </summary>
    [JsonPropertyName("textFormat")]
    public string? TextFormat
    {
        get => base.Properties.TryGetValue("textFormat", out var value) ? value?.ToString() : null;
        set => base.Properties["textFormat"] = value;
    }

    /// <summary>
    /// Gets or sets the attachment layout.
    /// </summary>
    [JsonPropertyName("attachmentLayout")]
    public string? AttachmentLayout
    {
        get => base.Properties.TryGetValue("attachmentLayout", out var value) ? value?.ToString() : null;
        set => base.Properties["attachmentLayout"] = value;
    }

    /// <summary>
    /// Gets or sets the importance. See <see cref="ImportanceLevels"/> for common values.
    /// </summary>
    [JsonPropertyName("importance")]
    public string? Importance
    {
        get => base.Properties.TryGetValue("importance", out var value) ? value?.ToString() : null;
        set => base.Properties["importance"] = value;
    }

    /// <summary>
    /// Gets or sets the delivery mode. See <see cref="DeliveryModes"/> for common values.
    /// </summary>
    [JsonPropertyName("deliveryMode")]
    public string? DeliveryMode
    {
        get => base.Properties.TryGetValue("deliveryMode", out var value) ? value?.ToString() : null;
        set => base.Properties["deliveryMode"] = value;
    }

    /// <summary>
    /// Gets or sets the expiration time of the message.
    /// </summary>
    [JsonPropertyName("expiration")]
    public DateTime? Expiration
    {
        get => base.Properties.TryGetValue("expiration", out var value) && value != null
            ? (DateTime.TryParse(value.ToString(), out var date) ? date : null)
            : null;
        set => base.Properties["expiration"] = value;
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
