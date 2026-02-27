// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Represents a response from a message extension action that can contain either a task module or compose extension response.
/// </summary>
public class MessageExtensionActionResponse
{
    /// <summary>
    /// The task module result.
    /// </summary>
    [JsonPropertyName("task")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Response? Task { get; set; }

    /// <summary>
    /// The compose extension result (for message extension results, auth, config, etc.).
    /// </summary>
    [JsonPropertyName("composeExtension")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ComposeExtension? ComposeExtension { get; set; }

    /// <summary>
    /// Creates a new builder for MessageExtensionActionResponse.
    /// </summary>
    public static MessageExtensionActionResponseBuilder CreateBuilder()
    {
        return new MessageExtensionActionResponseBuilder();
    }
}

/// <summary>
/// Builder for MessageExtensionActionResponse.
/// </summary>
public class MessageExtensionActionResponseBuilder
{
    private TaskModuleResponse? _taskResponse;
    private MessageExtensionResponse? _extensionResponse;

    /// <summary>
    /// Sets the task module response using a TaskModuleResponseBuilder.
    /// </summary>
    public MessageExtensionActionResponseBuilder WithTask(TaskModuleResponseBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        _taskResponse = builder.Validate();
        return this;
    }

    /// <summary>
    /// Sets the compose extension response using a MessageExtensionResponseBuilder.
    /// </summary>
    public MessageExtensionActionResponseBuilder WithComposeExtension(MessageExtensionResponseBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        _extensionResponse = builder.Validate();
        return this;
    }

    /// <summary>
    /// Validates and builds the MessageExtensionActionResponse.
    /// </summary>
    private MessageExtensionActionResponse Validate()
    {
        if (_taskResponse == null && _extensionResponse == null)
        {
            throw new InvalidOperationException("Either Task or ComposeExtension must be set. Use WithTask() or WithComposeExtension().");
        }

        if (_taskResponse != null && _extensionResponse != null)
        {
            throw new InvalidOperationException("Cannot set both Task and ComposeExtension. Use either WithTask() or WithComposeExtension(), not both.");
        }

        return new MessageExtensionActionResponse
        {
            Task = _taskResponse?.Task,
            ComposeExtension = _extensionResponse?.ComposeExtension
        };
    }

    /// <summary>
    /// Builds the MessageExtensionActionResponse and wraps it in a InvokeResponse.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (default: 200).</param>
    public InvokeResponse<MessageExtensionActionResponse> Build(int statusCode = 200)
    {
        return new InvokeResponse<MessageExtensionActionResponse>(statusCode, Validate());
    }
}
