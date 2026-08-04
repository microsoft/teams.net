// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Utils;

namespace Microsoft.Teams.Apps.TaskModules;

/// <summary>
/// Task module response types.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<TaskModuleResponseType>))]
public class TaskModuleResponseType(string value) : StringEnum(value)
{
    /// <summary>
    /// Continue type - displays a card or URL in the task module.
    /// </summary>
    public static readonly TaskModuleResponseType Continue = new("continue");

    /// <summary>
    /// Message type - displays a plain text message.
    /// </summary>
    public static readonly TaskModuleResponseType Message = new("message");
}

/// <summary>
/// Task module response types.
/// </summary>
public static class TaskModuleResponseTypes
{
    /// <summary>
    /// Continue type - displays a card or URL in the task module.
    /// </summary>
    public static TaskModuleResponseType Continue => TaskModuleResponseType.Continue;

    /// <summary>
    /// Message type - displays a plain text message.
    /// </summary>
    public static TaskModuleResponseType Message => TaskModuleResponseType.Message;
}

/// <summary>
/// Task module size constants.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<TaskModuleSize>))]
public class TaskModuleSize(string value) : StringEnum(value)
{
    /// <summary>Small task module size.</summary>
    public static readonly TaskModuleSize Small = new("small");
    /// <summary>Medium task module size.</summary>
    public static readonly TaskModuleSize Medium = new("medium");
    /// <summary>Large task module size.</summary>
    public static readonly TaskModuleSize Large = new("large");
}

/// <summary>
/// Task module size constants.
/// </summary>
public static class TaskModuleSizes
{
    /// <summary>
    /// Small size.
    /// </summary>
    public static TaskModuleSize Small => TaskModuleSize.Small;

    /// <summary>
    /// Medium size.
    /// </summary>
    public static TaskModuleSize Medium => TaskModuleSize.Medium;

    /// <summary>
    /// Large size.
    /// </summary>
    public static TaskModuleSize Large => TaskModuleSize.Large;
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
    private TaskModuleResponseType? _type;
    private string? _title;
    private TeamsAttachment? _card;
    private object _height = TaskModuleSizes.Small;
    private object _width = TaskModuleSizes.Small;
    private string? _message;

    /// <summary>
    /// Sets the type of the response. Use <see cref="TaskModuleResponseTypes"/> constants.
    /// </summary>
    public TaskModuleResponseBuilder WithType(TaskModuleResponseType type)
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
    /// Sets the card content for <see cref="TaskModuleResponseTypes.Continue"/> responses.
    /// </summary>
    public TaskModuleResponseBuilder WithCard(TeamsAttachment card)
    {
        _card = card;
        return this;
    }

    /// <summary>
    /// Sets the height. Can be a number (pixels) or use <see cref="TaskModuleSizes"/> constants.
    /// </summary>
    public TaskModuleResponseBuilder WithHeight(object height)
    {
        _height = height;
        return this;
    }

    /// <summary>
    /// Sets the width. Can be a number (pixels) or use <see cref="TaskModuleSizes"/> constants.
    /// </summary>
    public TaskModuleResponseBuilder WithWidth(object width)
    {
        _width = width;
        return this;
    }

    /// <summary>
    /// Sets the message for <see cref="TaskModuleResponseTypes.Message"/> responses.
    /// </summary>
    public TaskModuleResponseBuilder WithMessage(string message)
    {
        _message = message;
        return this;
    }

    /// <summary>
    /// Builds the TaskModuleResponse.
    /// </summary>
    internal TaskModuleResponse Validate()
    {
        if (_type is null)
        {
            throw new InvalidOperationException("Type must be set. Use WithType() to specify TaskModuleResponseTypes.Continue or TaskModuleResponseTypes.Message.");
        }

        object? value = _type.Value switch
        {
            "continue" => ValidateContinueType(),
            "message" => ValidateMessageType(),
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
            card = _card
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
    /// Builds the TaskModuleResponse and wraps it in an <see cref="InvokeResponse{TBody}"/>.
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
    /// Type of result. See <see cref="TaskModuleResponseTypes"/> for known values.
    /// </summary>
    [JsonPropertyName("type")]
    public required TaskModuleResponseType Type { get; set; }

    /// <summary>
    /// The result value.
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }
}
