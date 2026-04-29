// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Schema;

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
        Attachments = activity.Properties.Extract<IList<TeamsAttachment>>("attachments");
        Text = activity.Properties.Extract<string>("text");
        TextFormat = activity.Properties.Extract<string>("textFormat");
        AttachmentLayout = activity.Properties.Extract<string>("attachmentLayout");
        SuggestedActions = activity.Properties.Extract<SuggestedActions>("suggestedActions");

        /*
        if (activity.Properties.TryGetValue("speak", out var speak))
        {
            Speak = speak?.ToString();
            activity.Properties.Remove("speak");
        }
        if (activity.Properties.TryGetValue("inputHint", out var inputHint))
        {
            InputHint = inputHint?.ToString();
            activity.Properties.Remove("inputHint");
        }
        if (activity.Properties.TryGetValue("summary", out var summary))
        {
            Summary = summary?.ToString();
            activity.Properties.Remove("summary");
        }
        if (activity.Properties.TryGetValue("importance", out var importance))
        {
            Importance = importance?.ToString();
            activity.Properties.Remove("importance");
        }
        if (activity.Properties.TryGetValue("deliveryMode", out var deliveryMode))
        {
            DeliveryMode = deliveryMode?.ToString();
            activity.Properties.Remove("deliveryMode");
        }
        if (activity.Properties.TryGetValue("expiration", out var expiration))
        {
            Expiration = expiration?.ToString();
            activity.Properties.Remove("expiration");
        }
        */
    }

    /// <summary>
    /// Gets or sets the attachments for the message.
    /// </summary>
    [JsonPropertyName("attachments")]
    public IList<TeamsAttachment>? Attachments { get; set; }

    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

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
    /// Gets or sets the text format. See <see cref="TextFormats"/> for common values.
    /// </summary>
    [JsonPropertyName("textFormat")]
    public string? TextFormat { get; set; }

    /// <summary>
    /// Gets or sets the attachment layout.
    /// </summary>
    [JsonPropertyName("attachmentLayout")]
    public string? AttachmentLayout { get; set; }



    //TODO : Review properties
    /*
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
    public string? Expiration { get; set; }
    */

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


/*
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


*/
