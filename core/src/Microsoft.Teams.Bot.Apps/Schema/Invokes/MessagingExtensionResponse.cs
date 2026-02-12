// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema.Invokes;

/// <summary>
/// Messaging extension response types.
/// </summary>
public static class MessagingExtensionResponseType
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
public class MessagingExtensionResponse
{
    /// <summary>
    /// The compose extension result.
    /// </summary>
    [JsonPropertyName("composeExtension")]
    public ComposeExtension? ComposeExtension { get; set; }

    /// <summary>
    /// Creates a new builder for MessagingExtensionResponse.
    /// </summary>
    public static MessagingExtensionResponseBuilder CreateBuilder()
    {
        return new MessagingExtensionResponseBuilder();
    }
}


/// <summary>
/// Messaging extension result.
/// </summary>
public class ComposeExtension
{
    /// <summary>
    /// Type of result.
    /// See <see cref="MessagingExtensionResponseType"/> for common values.
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
    public MessagingExtensionSuggestedAction? SuggestedActions { get; set; }
}

/// <summary>
/// Suggested actions for messaging extension configuration.
/// </summary>
public class MessagingExtensionSuggestedAction
{
    /// <summary>
    /// Array of actions.
    /// </summary>
    [JsonPropertyName("actions")]
    public IList<MessagingExtensionAction>? Actions { get; set; }
}

/// <summary>
/// Action for messaging extension.
/// </summary>
//TODO : this should come from cards package
public class MessagingExtensionAction
{
    /// <summary>
    /// Type of action. Common values: "openUrl", "imBack".
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Value associated with the action.
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>
    /// Title to display for the action.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

/// <summary>
/// Builder for MessagingExtensionResponse.
/// </summary>
public class MessagingExtensionResponseBuilder
{
    private string? _type;
    private string? _attachmentLayout;
    private TeamsAttachment[]? _attachments;
    private TeamsActivity? _activityPreview;
    private MessagingExtensionAction[]? _suggestedActions;
    private string? _text;

    /// <summary>
    /// Sets the type of the response. Common values: "result", "auth", "config", "message", "botMessagePreview".
    /// </summary>
    public MessagingExtensionResponseBuilder WithType(string type)
    {
        _type = type;
        return this;
    }

    /// <summary>
    /// Sets the attachment layout. Common values: "list", "grid".
    /// </summary>
    public MessagingExtensionResponseBuilder WithAttachmentLayout(string layout)
    {
        _attachmentLayout = layout;
        return this;
    }

    /// <summary>
    /// Sets the attachments for the response.
    /// </summary>
    public MessagingExtensionResponseBuilder WithAttachments(params TeamsAttachment[] attachments)
    {
        _attachments = attachments;
        return this;
    }

    /// <summary>
    /// Sets the activity preview for bot message preview type.
    /// </summary>
    public MessagingExtensionResponseBuilder WithActivityPreview(TeamsActivity activityPreview)
    {
        _activityPreview = activityPreview;
        return this;
    }

    /// <summary>
    /// Sets suggested actions for config type.
    /// </summary>
    public MessagingExtensionResponseBuilder WithSuggestedActions(params MessagingExtensionAction[] actions)
    {
        _suggestedActions = actions;
        return this;
    }

    /// <summary>
    /// Sets the text message for message type.
    /// </summary>
    public MessagingExtensionResponseBuilder WithText(string text)
    {
        _text = text;
        return this;
    }

    /// <summary>
    /// Builds the MessagingExtensionResponse.
    /// </summary>
    public MessagingExtensionResponse Build()
    {
        return new MessagingExtensionResponse
        {
            ComposeExtension = new ComposeExtension
            {
                Type = _type,
                AttachmentLayout = _attachmentLayout,
                Attachments = _attachments,
                ActivityPreview = _activityPreview,
                SuggestedActions = _suggestedActions != null ? new MessagingExtensionSuggestedAction { Actions = _suggestedActions } : null,
                Text = _text
            }
        };
    }
}
