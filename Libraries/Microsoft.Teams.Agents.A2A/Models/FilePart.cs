using System.Text.Json.Serialization;

namespace Microsoft.Teams.Agents.A2A.Models;

public class FilePart : IPart
{
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public string Type => "file";

    [JsonPropertyName("file")]
    [JsonPropertyOrder(1)]
    public required FileContent File { get; set; }

    [JsonPropertyName("metadata")]
    [JsonPropertyOrder(2)]
    public IDictionary<string, object>? MetaData { get; set; }
}