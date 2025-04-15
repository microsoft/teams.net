using System.Text.Json.Serialization;

namespace Microsoft.Teams.Agents.A2A.Models;

public class TaskStatusUpdateEvent
{
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    [JsonPropertyName("status")]
    [JsonPropertyOrder(1)]
    public required TaskStatus Status { get; set; }

    [JsonPropertyName("final")]
    [JsonPropertyOrder(2)]
    public required bool Final { get; set; }

    [JsonPropertyName("metadata")]
    [JsonPropertyOrder(3)]
    public IDictionary<string, object?> MetaData { get; set; } = new Dictionary<string, object?>();
}