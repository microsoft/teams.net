// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.AI.Messages;

namespace Microsoft.Teams.AI.Models;

/// <summary>
/// a model that can reason over and
/// respond with text
/// </summary>
public interface IChatModel<TOptions> : IModel<TOptions>
{
    /// <summary>
    /// send a message to the model
    /// </summary>
    /// <param name="message">the message to send</param>
    /// <param name="options">the options</param>
    /// <returns>the models response</returns>
    public Task<ModelMessage<string>> Send(IMessage message, ChatModelOptions<TOptions> options, CancellationToken cancellationToken = default);

    /// <summary>
    /// send a message to the model and stream
    /// the response
    /// </summary>
    /// <param name="message">the message to send</param>
    /// <param name="options">the options</param>
    /// <param name="stream">the stream to use</param>
    /// <returns>the models response</returns>
    public Task<ModelMessage<string>> Send(IMessage message, ChatModelOptions<TOptions> options, IStream stream, CancellationToken cancellationToken = default);
}

/// <summary>
/// options to send with the message
/// </summary>
public class ChatModelOptions<TOptions>
{
    /// <summary>
    /// the initial prompt message that defines
    /// model behavior
    /// </summary>
    public DeveloperMessage? Prompt { get; set; }

    /// <summary>
    /// the conversation history
    /// </summary>
    public IList<IMessage> Messages { get; set; } = [];

    /// <summary>
    /// the registered functions that can be
    /// called
    /// </summary>
    public required IList<IFunction> Functions { get; set; }

    /// <summary>
    /// the request options defined by the model
    /// </summary>
    public TOptions? Options { get; set; }

    /// <summary>
    /// the handler used to invoke functions
    /// </summary>
    internal Func<FunctionCall, CancellationToken, Task<object?>>? OnInvoke;

    public ChatModelOptions(Func<FunctionCall, CancellationToken, Task<object?>>? onInvoke = null)
    {
        OnInvoke = onInvoke;
    }

    /// <summary>
    /// invoke a function
    /// </summary>
    /// <param name="call">the function call</param>
    /// <returns>the function response</returns>
    public Task<object?> Invoke(FunctionCall call, CancellationToken cancellationToken = default)
    {
        return OnInvoke is null ? Task.FromResult<object?>(null) : OnInvoke(call, cancellationToken);
    }
}