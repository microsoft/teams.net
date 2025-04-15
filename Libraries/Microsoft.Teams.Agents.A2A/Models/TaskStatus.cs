using System.Text.Json.Serialization;

namespace Microsoft.Teams.Agents.A2A.Models;

public class TaskStatus
{
    [JsonPropertyName("state")]
    [JsonPropertyOrder(0)]
    public required TaskState State { get; set; }

    [JsonPropertyName("message")]
    [JsonPropertyOrder(1)]
    public Message? Message { get; set; }

    [JsonPropertyName("timestamp")]
    [JsonPropertyOrder(2)]
    public DateTime? Timestamp { get; set; }
}