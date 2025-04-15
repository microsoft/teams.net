using Microsoft.Teams.Common.Json;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools;

[TrueTypeJson<IEvent>]
public interface IEvent
{
    public Guid Id { get; }
    public string Type { get; }
    public object? Body { get; }
    public DateTime SentAt { get; }
}