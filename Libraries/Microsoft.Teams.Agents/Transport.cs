namespace Microsoft.Teams.Agents;

public delegate void Ack();

/// <summary>
/// handles how messages between
/// agents are sent/received
/// </summary>
public interface ITransport
{
    public Task Send(IMessage message, CancellationToken cancellationToken = default);
    public void Ack(string id);

    public void OnMessage(Action<IMessage, Ack> onMessage);
    public void OnMessage(Func<IMessage, Ack, Task> onMessage);
}