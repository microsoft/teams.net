using Microsoft.Teams.Common.Json;

namespace Microsoft.Teams.Agents.A2A.Models;

[TrueTypeJson<IPart>]
public interface IPart
{
    public string Type { get; }
    public IDictionary<string, object>? MetaData { get; }
}