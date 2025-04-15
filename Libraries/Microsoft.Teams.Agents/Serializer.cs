namespace Microsoft.Teams.Agents;

/// <summary>
/// handles how messages are
/// serialized/deserialized
/// </summary>
public interface ISerializer
{
    public string Serialize(IMessage message);
    public string? TrySerialize(IMessage message);
    public IMessage Deserialize(string payload);
    public IMessage? TryDeserialize(string payload);
}