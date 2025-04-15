using System.Text.Json.Serialization;

namespace Microsoft.Teams.Agents.A2A.Models;

public class Task
{
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("sessionId")]
    [JsonPropertyOrder(1)]
    public string? SessionId { get; set; }

    [JsonPropertyName("status")]
    [JsonPropertyOrder(2)]
    public required TaskStatus Status { get; set; }

    [JsonPropertyName("history")]
    [JsonPropertyOrder(2)]
    public IList<Message>? History { get; set; }

    [JsonPropertyName("artifacts")]
    [JsonPropertyOrder(3)]
    public IList<Artifact>? Artifacts { get; set; }

    [JsonPropertyName("metadata")]
    [JsonPropertyOrder(4)]
    public IDictionary<string, object?> MetaData { get; set; } = new Dictionary<string, object?>();
}