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
    /// <param name="args">the arguments</param>
    /// <returns>the transformed arguments</returns>
    public Task<TArgs> OnBeforeFunctionCall<TOptions, TArgs>(IChatPrompt<TOptions> prompt, IFunction function, TArgs args, CancellationToken cancellationToken = default);

    /// <summary>
    /// called after a prompt function is called
    /// </summary>
    /// <param name="prompt">the prompt</param>
    /// <param name="function">the function</param>
    /// <param name="output">the functions return value</param>
    /// <returns>the transformed response</returns>
    public Task<TArgs> OnAfterFunctionCall<TOptions, TArgs>(IChatPrompt<TOptions> prompt, IFunction function, TArgs output, CancellationToken cancellationToken = default);
}