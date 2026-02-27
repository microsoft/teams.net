// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Task module response types.
/// </summary>
public static class TaskModuleResponseType
{
    /// <summary>
    /// Continue type - displays a card or URL in the task module.
    /// </summary>
    public const string Continue = "continue";

    /// <summary>
    /// Message type - displays a plain text message.
    /// </summary>
    public const string Message = "message";
}

/// <summary>
/// Task module size constants.
/// </summary>
public static class TaskModuleSize
{
    /// <summary>
    /// Small size.
    /// </summary>
    public const string Small = "small";

    /// <summary>
    /// Medium size.
    /// </summary>
    public const string Medium = "medium";

    /// <summary>
    /// Large size.
    /// </summary>
    public const string Large = "large";
}

/// <summary>
/// Task module response wrapper.
/// </summary>
public class TaskModuleResponse
{
    /// <summary>
    /// The task module result.
    /// </summary>
    [JsonPropertyName("task")]
    public Response? Task { get; set; }

    /// <summary>
    /// Creates a new builder for TaskModuleResponse.
    /// </summary>
    public static TaskModuleResponseBuilder CreateBuilder()
    {
        return new TaskModuleResponseBuilder();
    }
}

/// <summary>
/// Builder for TaskModuleResponse.
/// </summary>
public class TaskModuleResponseBuilder
{
    private string? _type;
    private string? _title;
    private object? _card;
    private object _height = TaskModuleSize.Small;
    private object _width = TaskModuleSize.Small;
    private string? _message;
    //private string? _url;
    //private string? _fallbackUrl;
    //private string? _completionBotId;

    /// <summary>
    /// Sets the type of the response. Use TaskModuleResponseType constants.
    /// </summary>
    public TaskModuleResponseBuilder WithType(string type)
    {
        _type = type;
        return this;
    }

    /// <summary>
    /// Sets the title of the task module.
    /// </summary>
    public TaskModuleResponseBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>
    /// Sets the card content for continue type.
    /// </summary>
    public TaskModuleResponseBuilder WithCard(object card)
    {
        _card = card;
        return this;
    }

    /// <summary>
    /// Sets the height. Can be a number (pixels) or use TaskModuleSize constants.
    /// </summary>
    public TaskModuleResponseBuilder WithHeight(object height)
    {
        _height = height;
        return this;
    }

    /// <summary>
    /// Sets the width. Can be a number (pixels) or use TaskModuleSize constants.
    /// </summary>
    public TaskModuleResponseBuilder WithWidth(object width)
    {
        _width = width;
        return this;
    }

    /// <summary>
    /// Sets the message for message type.
    /// </summary>
    public TaskModuleResponseBuilder WithMessage(string message)
    {
        _message = message;
        return this;
    }

    /*
     /// <summary>
    /// Sets the URL for continue type.
    /// </summary>
    public TaskModuleResponseBuilder WithUrl(string url)
    {
        _url = url;
        return this;
    }

    /// <summary>
    /// Sets the fallback URL if the card cannot be displayed.
    /// </summary>
    public TaskModuleResponseBuilder WithFallbackUrl(string fallbackUrl)
    {
        _fallbackUrl = fallbackUrl;
        return this;
    }

    /// <summary>
    /// Sets the completion bot ID.
    /// </summary>
    public TaskModuleResponseBuilder WithCompletionBotId(string completionBotId)
    {
        _completionBotId = completionBotId;
        return this;
    }
    */

    /// <summary>
    /// Builds the TaskModuleResponse.
    /// </summary>
    internal TaskModuleResponse Validate()
    {
        if (string.IsNullOrEmpty(_type))
        {
            throw new InvalidOperationException("Type must be set. Use WithType() to specify TaskModuleResponseType.Continue or TaskModuleResponseType.Message.");
        }

        object? value = _type switch
        {
            TaskModuleResponseType.Continue => ValidateContinueType(),
            TaskModuleResponseType.Message => ValidateMessageType(),
            _ => throw new InvalidOperationException($"Unknown task module response type: {_type}")
        };

        return new TaskModuleResponse
        {
            Task = new Response
            {
                Type = _type,
                Value = value
            }
        };
    }

    private object ValidateContinueType()
    {
        if (_card == null)
        {
            throw new InvalidOperationException("Card must be set for Continue type. Use WithCard().");
        }

        if (!string.IsNullOrEmpty(_message))
        {
            throw new InvalidOperationException("Message cannot be set for Continue type. Message is only used with Message type.");
        }

        return new
        {
            title = _title,
            height = _height,
            width = _width,
            card = _card,
            //url = _url,
            //fallbackUrl = _fallbackUrl,
            //completionBotId = _completionBotId
        };
    }

    private string ValidateMessageType()
    {
        if (string.IsNullOrEmpty(_message))
        {
            throw new InvalidOperationException("Message must be set for Message type. Use WithMessage().");
        }

        if (!string.IsNullOrEmpty(_title))
        {
            throw new InvalidOperationException("Title cannot be set for Message type. Title is only used with Continue type.");
        }

        if (_card != null)
        {
            throw new InvalidOperationException("Card cannot be set for Message type. Card is only used with Continue type.");
        }

        return _message;
    }

    /// <summary>
    /// Builds the TaskModuleResponse and wraps it in a InvokeResponse.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (default: 200).</param>
    public InvokeResponse<TaskModuleResponse> Build(int statusCode = 200)
    {
        return new InvokeResponse<TaskModuleResponse>(statusCode, Validate());
    }
}

/// <summary>
/// Task module result.
/// </summary>
public class Response
{
    /// <summary>
    /// Type of result.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Value 
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }
}
