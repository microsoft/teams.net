using System.Text.Json.Serialization;

namespace Microsoft.Teams.Agents.A2A.Models;

public class TaskArtifactUpdateEvent
{
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    [JsonPropertyName("artifact")]
    [JsonPropertyOrder(1)]
    public required Artifact Artifact { get; set; }

    [JsonPropertyName("metadata")]
    [JsonPropertyOrder(2)]
    public IDictionary<string, object?> MetaData { get; set; } = new Dictionary<string, object?>();
}