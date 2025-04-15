namespace Microsoft.Teams.AI;

/// <summary>
/// models act as the communication driver
/// or connection to one or more LLM's, either remotely or
/// locally.
/// </summary>
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