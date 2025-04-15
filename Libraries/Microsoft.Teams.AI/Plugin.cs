namespace Microsoft.Teams.AI;

/// <summary>
/// a reusable component that can
/// change the way a prompt works
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// called before a prompt sends
    /// a message
    /// </summary>
    /// <param name="prompt">the prompt</param>
    /// <param name="message">the message</param>
    /// <param name="options">the model options</param>
    /// <returns>the transformed message</returns>
    public Task<IMessage> OnBeforeSend<TOptions>(IPrompt<TOptions> prompt, IMessage message, TOptions? options = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// called after a prompt sends
    /// a message
    /// </summary>
    /// <param name="prompt">the prompt</param>
    /// <param name="message">the message</param>
    /// <param name="options">the model options</param>
    /// <returns>the transformed message</returns>
    public Task<IMessage> OnAfterSend<TOptions>(IPrompt<TOptions> prompt, IMessage message, TOptions? options = default, CancellationToken cancellationToken = default);
}