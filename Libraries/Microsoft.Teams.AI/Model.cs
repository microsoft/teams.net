// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.AI;

/// <summary>
/// models act as the communication driver
/// or connection to one or more LLM's, either remotely or
/// locally.
/// </summary>
[Obsolete("Microsoft.Teams.AI is deprecated and will be removed by end of summer 2026.")]
public interface IModel<TOptions>
{
    /// <summary>
    /// the model name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// send a message to the model
    /// </summary>
    /// <param name="message">the message to send</param>
    /// <returns>the models response</returns>
    public Task<IMessage> Send(IMessage message, TOptions? options = default, CancellationToken cancellationToken = default);
}