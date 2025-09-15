// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.AI.Prompts;

namespace Microsoft.Teams.AI;

/// <summary>
/// a component that can change the
/// way a ChatPrompt works
/// </summary>
public interface IChatPlugin
{
    /// <summary>
    /// called before a prompt sends
    /// a message
    /// </summary>
    /// <param name="prompt">the prompt</param>
    /// <param name="message">the message</param>
    /// <param name="options">the model options</param>
    /// <returns>the transformed message</returns>
    public Task<IMessage> OnBeforeSend<TOptions>(IChatPrompt<TOptions> prompt, IMessage message, TOptions? options = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// called after a prompt sends
    /// a message
    /// </summary>
    /// <param name="prompt">the prompt</param>
    /// <param name="message">the message</param>
    /// <param name="options">the model options</param>
    /// <returns>the transformed message</returns>
    public Task<IMessage> OnAfterSend<TOptions>(IChatPrompt<TOptions> prompt, IMessage message, TOptions? options = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// called before a prompt function is called
    /// </summary>
    /// <param name="prompt">the prompt</param>
    /// <param name="function">the function</param>
    /// <param name="call">the function call</param>
    /// <returns>the transformed call</returns>
    public Task<FunctionCall> OnBeforeFunctionCall<TOptions>(IChatPrompt<TOptions> prompt, IFunction function, FunctionCall call, CancellationToken cancellationToken = default);

    /// <summary>
    /// called after a prompt function is called
    /// </summary>
    /// <param name="prompt">the prompt</param>
    /// <param name="function">the function</param>
    /// <param name="call">the function call</param>
    /// <param name="output">the functions return value</param>
    /// <returns>the transformed response</returns>
    public Task<object?> OnAfterFunctionCall<TOptions>(IChatPrompt<TOptions> prompt, IFunction function, FunctionCall call, object? output, CancellationToken cancellationToken = default);

    /// <summary>
    /// Modify the prompt functions passed to the model.
    /// </summary>
    /// <param name="prompt">the prompt</param>
    /// <param name="functions">a copy of the configured chat prompt functions</param>
    /// <param name="cancellationToken">the cancellation token</param>
    /// <returns>the transformed functions</returns>
    public Task<FunctionCollection> OnBuildFunctions<TOptions>(IChatPrompt<TOptions> prompt, FunctionCollection functions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Modify the prompt instructions passed to the model.
    /// </summary>
    /// <param name="prompt">the prompt</param>
    /// <param name="instructions">the instructions</param>
    /// <returns>the transformed instructions</returns>
    public Task<DeveloperMessage?> OnBuildInstructions<TOptions>(IChatPrompt<TOptions> prompt, DeveloperMessage? instructions);
}