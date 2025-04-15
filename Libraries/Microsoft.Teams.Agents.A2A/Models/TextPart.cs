using System.Text.Json.Serialization;

namespace Microsoft.Teams.Agents.A2A.Models;

public class TextPart : IPart
{
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public string Type => "text";

    [JsonPropertyName("text")]
    [JsonPropertyOrder(1)]
    public required string Text { get; set; }

    [JsonPropertyName("metadata")]
    [JsonPropertyOrder(2)]
    public IDictionary<string, object>? MetaData { get; set; }
}