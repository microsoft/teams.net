// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema.Invokes;

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
    public TaskResponse? Task { get; set; }

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
    public TaskModuleResponse Build()
    {
        object? value = _type switch
        {
            TaskModuleResponseType.Continue => new
            {
                title = _title,
                height = _height,
                width = _width,
                card = _card,
                //url = _url,
                //fallbackUrl = _fallbackUrl,
                //completionBotId = _completionBotId
            },
            TaskModuleResponseType.Message => _message,
            _ => null
        };

        return new TaskModuleResponse
        {
            Task = new TaskResponse
            {
                Type = _type,
                Value = value
            }
        };
    }
}

/// <summary>
/// Task module result.
/// </summary>
public class TaskResponse
{
    /// <summary>
    /// Type of result.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Value 
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }
}

/// <summary>
/// Task module continue response value.
/// </summary>
public class TaskModuleContinueResponse
{
    /// <summary>
    /// Title of the task module.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Height of the task module. Can be a number (pixels) or "small", "medium", "large".
    /// </summary>
    [JsonPropertyName("height")]
    public object? Height { get; set; }

    /// <summary>
    /// Width of the task module. Can be a number (pixels) or "small", "medium", "large".
    /// </summary>
    [JsonPropertyName("width")]
    public object? Width { get; set; }

    /// <summary>
    /// Card to display in the task module.
    /// </summary>
    [JsonPropertyName("card")]
    public TaskModuleCardResponse? Card { get; set; }

    //TODO : Review 
    /*
    /// <summary>
    /// URL to display in an iframe.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Fallback URL if the card cannot be displayed.
    /// </summary>
    [JsonPropertyName("fallbackUrl")]
    public string? FallbackUrl { get; set; }

    /// <summary>
    /// Completion bot ID.
    /// </summary>
    [JsonPropertyName("completionBotId")]
    public string? CompletionBotId { get; set; }
    */
}

/// <summary>
/// Task module card response.
/// </summary>
public class TaskModuleCardResponse
{
    /// <summary>
    /// Content type of the card. Common value: "application/vnd.microsoft.card.adaptive".
    /// </summary>
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    /// <summary>
    /// Content of the card (the actual adaptive card).
    /// </summary>
    [JsonPropertyName("content")]
    public object? Content { get; set; }
}

/// <summary>
/// Task module message response (for type "message").
/// </summary>
public class TaskModuleMessageResponse
{
    /// <summary>
    /// Message to display.
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
