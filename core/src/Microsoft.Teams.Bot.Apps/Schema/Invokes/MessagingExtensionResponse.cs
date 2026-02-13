// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema.Invokes;

/// <summary>
/// Messaging extension response types.
/// </summary>
public static class MessageExtensionResponseType
{
    /// <summary>
    /// Result type - displays a list of search results.
    /// </summary>
    public const string Result = "result";

    /// <summary>
    /// Auth type - prompts the user to authenticate.
    /// </summary>
    public const string Auth = "auth";

    /// <summary>
    /// Config type - prompts the user to set up the message extension.
    /// </summary>
    public const string Config = "config";

    /// <summary>
    /// Message type - displays a plain text message.
    /// </summary>
    public const string Message = "message";

    /// <summary>
    /// Bot message preview type - shows a preview that can be edited before sending.
    /// </summary>
    public const string BotMessagePreview = "botMessagePreview";
}

/// <summary>
/// Messaging extension response wrapper.
/// </summary>
public class MessageExtensionResponse
{
    /// <summary>
    /// The compose extension result.
    /// </summary>
    [JsonPropertyName("composeExtension")]
    public ComposeExtension? ComposeExtension { get; set; }

    /// <summary>
    /// Creates a new builder for MessagingExtensionResponse.
    /// </summary>
    public static MessageExtensionResponseBuilder CreateBuilder()
    {
        return new MessageExtensionResponseBuilder();
    }
}


/// <summary>
/// Messaging extension result.
/// </summary>
public class ComposeExtension
{
    /// <summary>
    /// Type of result.
    /// See <see cref="MessageExtensionResponseType"/> for common values.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Layout for attachments.
    /// See <see cref="TeamsAttachmentLayout"/> for common values. 
    /// </summary>
    [JsonPropertyName("attachmentLayout")]
    public string? AttachmentLayout { get; set; }

    /// <summary>
    /// Array of attachments (cards) to display.
    /// </summary>
    // TODO : there is an extra preview field but when is it used ?
    [JsonPropertyName("attachments")]
    public IList<TeamsAttachment>? Attachments { get; set; }

    /// <summary>
    /// Text to display.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Activity preview for bot message preview.
    /// </summary>
    //TODO : this needs to be activity type or something else - format is type, attachments[]
    [JsonPropertyName("activityPreview")]
    public TeamsActivity? ActivityPreview { get; set; }

    /// <summary>
    /// Suggested actions for config type.
    /// </summary>
    [JsonPropertyName("suggestedActions")]
    public MessageExtensionSuggestedAction? SuggestedActions { get; set; }
}

/// <summary>
/// Suggested actions for messaging extension configuration.
/// </summary>
public class MessageExtensionSuggestedAction
{
    //TODO : this should come from cards package

    /// <summary>
    /// Array of actions.
    /// </summary>
    [JsonPropertyName("actions")]
    public IList<object>? Actions { get; set; }
}


/// <summary>
/// Builder for MessagingExtensionResponse.
/// </summary>
public class MessageExtensionResponseBuilder
{
    private string? _type;
    private string? _attachmentLayout;
    private TeamsAttachment[]? _attachments;
    private TeamsActivity? _activityPreview;
    private object[]? _suggestedActions;
    private string? _text;

    /// <summary>
    /// Sets the type of the response. Common values: "result", "auth", "config", "message", "botMessagePreview".
    /// </summary>
    public MessageExtensionResponseBuilder WithType(string type)
    {
        _type = type;
        return this;
    }

    /// <summary>
    /// Sets the attachment layout. Common values: "list", "grid".
    /// </summary>
    public MessageExtensionResponseBuilder WithAttachmentLayout(string layout)
    {
        _attachmentLayout = layout;
        return this;
    }

    /// <summary>
    /// Sets the attachments for the response.
    /// </summary>
    public MessageExtensionResponseBuilder WithAttachments(params TeamsAttachment[] attachments)
    {
        _attachments = attachments;
        return this;
    }

    /// <summary>
    /// Sets the activity preview for bot message preview type.
    /// </summary>
    public MessageExtensionResponseBuilder WithActivityPreview(TeamsActivity activityPreview)
    {
        _activityPreview = activityPreview;
        return this;
    }

    /// <summary>
    /// Sets suggested actions for config type.
    /// </summary>
    public MessageExtensionResponseBuilder WithSuggestedActions(params object[] actions)
    {
        _suggestedActions = actions;
        return this;
    }

    /// <summary>
    /// Sets the text message for message type.
    /// </summary>
    public MessageExtensionResponseBuilder WithText(string text)
    {
        _text = text;
        return this;
    }

    /// <summary>
    /// Builds the MessagingExtensionResponse.
    /// </summary>
    public MessageExtensionResponse Build()
    {
        return new MessageExtensionResponse
        {
            ComposeExtension = new ComposeExtension
            {
                Type = _type,
                AttachmentLayout = _attachmentLayout,
                Attachments = _attachments,
                ActivityPreview = _activityPreview,
                SuggestedActions = _suggestedActions != null ? new MessageExtensionSuggestedAction { Actions = _suggestedActions } : null,
                Text = _text
            }
        };
    }
}