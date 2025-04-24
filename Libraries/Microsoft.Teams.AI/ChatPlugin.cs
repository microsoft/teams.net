using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.AI.Prompts;

namespace Microsoft.Teams.AI;

/// <summary>
/// a component that can change the
/// way a ChatPrompt works
/// </summary>
public interface IChatPlugin : IPlugin
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
}