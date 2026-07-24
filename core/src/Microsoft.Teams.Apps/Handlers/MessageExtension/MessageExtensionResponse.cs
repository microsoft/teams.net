// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Utils;

namespace Microsoft.Teams.Apps.MessageExtension;

/// <summary>
/// Messaging extension response types.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<MessageExtensionResponseType>))]
public class MessageExtensionResponseType(string value) : StringEnum(value)
{
    /// <summary>Result response type.</summary>
    public static readonly MessageExtensionResponseType Result = new("result");
    /// <summary>Message response type.</summary>
    public static readonly MessageExtensionResponseType Message = new("message");
    /// <summary>Bot message preview response type.</summary>
    public static readonly MessageExtensionResponseType BotMessagePreview = new("botMessagePreview");
    /// <summary>Config response type.</summary>
    public static readonly MessageExtensionResponseType Config = new("config");
}

/// <summary>
/// Messaging extension response types.
/// </summary>
public static class MessageExtensionResponseTypes
{
    /// <summary>
    /// Result type - displays a list of search results.
    /// </summary>
    public static MessageExtensionResponseType Result => MessageExtensionResponseType.Result;

    /// <summary>
    /// Message type - displays a plain text message.
    /// </summary>
    public static MessageExtensionResponseType Message => MessageExtensionResponseType.Message;

    /// <summary>
    /// Bot message preview type - shows a preview that can be edited before sending.
    /// </summary>
    public static MessageExtensionResponseType BotMessagePreview => MessageExtensionResponseType.BotMessagePreview;

    /// <summary>
    /// Config type - prompts the user to set up the message extension.
    /// </summary>
    public static MessageExtensionResponseType Config => MessageExtensionResponseType.Config;
}

/// <summary>
/// Messaging extension response wrapper.
/// </summary>
public class MessageExtensionResponse
{
    /// <summary>
    /// The compose extension result (for message extension results, auth, config, etc.).
    /// </summary>
    [JsonPropertyName("composeExtension")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ComposeExtension? ComposeExtension { get; set; }

    /// <summary>
    /// Creates a new builder for MessageExtensionResponse.
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
    /// See <see cref="MessageExtensionResponseTypes"/> for common values.
    /// </summary>
    [JsonPropertyName("type")]
    public MessageExtensionResponseType? Type { get; set; }

    /// <summary>
    /// Layout for attachments.
    /// See <see cref="TeamsAttachmentLayouts"/> for common values.
    /// </summary>
    [JsonPropertyName("attachmentLayout")]
    public AttachmentLayoutType? AttachmentLayout { get; set; }

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
    public TeamsActivityInput? ActivityPreview { get; set; }

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
    /// <summary>
    /// Array of actions.
    /// </summary>
    [JsonPropertyName("actions")]
    public IList<SuggestedAction>? Actions { get; set; }
}


/// <summary>
/// Builder for MessageExtensionResponse.
/// </summary>
public class MessageExtensionResponseBuilder
{
    private MessageExtensionResponseType? _type;
    private AttachmentLayoutType? _attachmentLayout;
    private TeamsAttachment[]? _attachments;
    private TeamsActivityInput? _activityPreview;
    private SuggestedAction[]? _suggestedActions;
    private string? _text;

    /// <summary>
    /// Sets the type of the response. See <see cref="MessageExtensionResponseTypes"/> for known values.
    /// </summary>
    public MessageExtensionResponseBuilder WithType(MessageExtensionResponseType type)
    {
        _type = type;
        return this;
    }

    /// <summary>
    /// Sets the attachment layout. See <see cref="TeamsAttachmentLayouts"/> for known values.
    /// </summary>
    public MessageExtensionResponseBuilder WithAttachmentLayout(AttachmentLayoutType layout)
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
    /// Sets the activity preview for <see cref="MessageExtensionResponseTypes.BotMessagePreview"/> responses.
    /// </summary>
    public MessageExtensionResponseBuilder WithActivityPreview(TeamsActivityInput activityPreview)
    {
        _activityPreview = activityPreview;
        return this;
    }

    /// <summary>
    /// Sets suggested actions for <see cref="MessageExtensionResponseTypes.Config"/> responses.
    /// </summary>
    public MessageExtensionResponseBuilder WithSuggestedActions(params SuggestedAction[] actions)
    {
        _suggestedActions = actions;
        return this;
    }

    /// <summary>
    /// Sets the text message for <see cref="MessageExtensionResponseTypes.Message"/> responses.
    /// </summary>
    public MessageExtensionResponseBuilder WithText(string text)
    {
        _text = text;
        return this;
    }

    /// <summary>
    /// Validates and builds the MessageExtensionResponse.
    /// </summary>
    internal MessageExtensionResponse Validate()
    {
        if (_type is null)
        {
            throw new InvalidOperationException("Type must be set. Use WithType() to specify MessageExtensionResponseTypes.Result, Message, BotMessagePreview, or Config.");
        }

        return _type.Value switch
        {
            "result" => ValidateResultType(),
            "message" => ValidateMessageType(),
            "botMessagePreview" => ValidateBotMessagePreviewType(),
            "config" => ValidateConfigType(),
            _ => throw new InvalidOperationException($"Unknown message extension response type: {_type}")
        };
    }

    private MessageExtensionResponse ValidateResultType()
    {
        if (_attachments == null || _attachments.Length == 0)
        {
            throw new InvalidOperationException("Attachments must be set for Result type. Use WithAttachments().");
        }

        if (!string.IsNullOrEmpty(_text))
        {
            throw new InvalidOperationException("Text cannot be set for Result type. Text is only used with Message type.");
        }

        if (_activityPreview != null)
        {
            throw new InvalidOperationException("ActivityPreview cannot be set for Result type. ActivityPreview is only used with BotMessagePreview type.");
        }

        if (_suggestedActions != null)
        {
            throw new InvalidOperationException("SuggestedActions cannot be set for Result type. SuggestedActions is only used with Config type.");
        }

        return new MessageExtensionResponse
        {
            ComposeExtension = new ComposeExtension
            {
                Type = _type,
                AttachmentLayout = _attachmentLayout,
                Attachments = _attachments
            }
        };
    }

    private MessageExtensionResponse ValidateMessageType()
    {
        if (string.IsNullOrEmpty(_text))
        {
            throw new InvalidOperationException("Text must be set for Message type. Use WithText().");
        }

        if (_attachments != null)
        {
            throw new InvalidOperationException("Attachments cannot be set for Message type. Attachments is only used with Result or BotMessagePreview type.");
        }

        if (_attachmentLayout is not null)
        {
            throw new InvalidOperationException("AttachmentLayout cannot be set for Message type. AttachmentLayout is only used with Result type.");
        }

        if (_activityPreview != null)
        {
            throw new InvalidOperationException("ActivityPreview cannot be set for Message type. ActivityPreview is only used with BotMessagePreview type.");
        }

        if (_suggestedActions != null)
        {
            throw new InvalidOperationException("SuggestedActions cannot be set for Message type. SuggestedActions is only used with Config type.");
        }

        return new MessageExtensionResponse
        {
            ComposeExtension = new ComposeExtension
            {
                Type = _type,
                Text = _text
            }
        };
    }

    private MessageExtensionResponse ValidateBotMessagePreviewType()
    {
        if (_activityPreview == null)
        {
            throw new InvalidOperationException("ActivityPreview must be set for BotMessagePreview type. Use WithActivityPreview().");
        }

        if (!string.IsNullOrEmpty(_text))
        {
            throw new InvalidOperationException("Text cannot be set for BotMessagePreview type. Text is only used with Message type.");
        }

        if (_attachmentLayout is not null)
        {
            throw new InvalidOperationException("AttachmentLayout cannot be set for BotMessagePreview type. AttachmentLayout is only used with Result type.");
        }

        if (_suggestedActions != null)
        {
            throw new InvalidOperationException("SuggestedActions cannot be set for BotMessagePreview type. SuggestedActions is only used with Config type.");
        }

        return new MessageExtensionResponse
        {
            ComposeExtension = new ComposeExtension
            {
                Type = _type,
                ActivityPreview = _activityPreview,
                Attachments = _attachments
            }
        };
    }

    private MessageExtensionResponse ValidateConfigType()
    {
        if (_suggestedActions == null || _suggestedActions.Length == 0)
        {
            throw new InvalidOperationException("SuggestedActions must be set for Config type. Use WithSuggestedActions().");
        }

        if (_attachments != null)
        {
            throw new InvalidOperationException("Attachments cannot be set for Config type. Attachments is only used with Result or BotMessagePreview type.");
        }

        if (_attachmentLayout is not null)
        {
            throw new InvalidOperationException("AttachmentLayout cannot be set for Config type. AttachmentLayout is only used with Result type.");
        }

        if (!string.IsNullOrEmpty(_text))
        {
            throw new InvalidOperationException("Text cannot be set for Config type. Text is only used with Message type.");
        }

        if (_activityPreview != null)
        {
            throw new InvalidOperationException("ActivityPreview cannot be set for Config type. ActivityPreview is only used with BotMessagePreview type.");
        }

        return new MessageExtensionResponse
        {
            ComposeExtension = new ComposeExtension
            {
                Type = _type,
                SuggestedActions = new MessageExtensionSuggestedAction { Actions = _suggestedActions }
            }
        };
    }

    /// <summary>
    /// Builds the MessageExtensionResponse and wraps it in an <see cref="InvokeResponse{TBody}"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (default: 200).</param>
    public InvokeResponse<MessageExtensionResponse> Build(int statusCode = 200)
    {
        return new InvokeResponse<MessageExtensionResponse>(statusCode, Validate());
    }
}
