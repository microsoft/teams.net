using System.Text.Json.Serialization;

namespace Microsoft.Teams.Agents.A2A.Models;

public class Artifact
{
    [JsonPropertyName("name")]
    [JsonPropertyOrder(0)]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    [JsonPropertyOrder(1)]
    public string? Description { get; set; }

    [JsonPropertyName("parts")]
    [JsonPropertyOrder(2)]
    public IList<IPart> Parts { get; set; } = [];

    [JsonPropertyName("metadata")]
    [JsonPropertyOrder(3)]
    public IDictionary<string, object>? MetaData { get; set; }

    [JsonPropertyName("index")]
    [JsonPropertyOrder(4)]
    public required int Index { get; set; }

    [JsonPropertyName("append")]
    [JsonPropertyOrder(5)]
    public bool? Append { get; set; }

    [JsonPropertyName("lastChunk")]
    [JsonPropertyOrder(6)]
    public bool? LastChunk { get; set; }
}