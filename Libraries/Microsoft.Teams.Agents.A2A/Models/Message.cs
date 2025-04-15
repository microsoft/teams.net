using System.Text.Json.Serialization;

namespace Microsoft.Teams.Agents.A2A.Models;

public class Message
{
    [JsonPropertyName("role")]
    [JsonPropertyOrder(0)]
    public required string Role { get; set; }

    [JsonPropertyName("parts")]
    [JsonPropertyOrder(1)]
    public IList<IPart> Parts { get; set; } = [];

    [JsonPropertyName("metadata")]
    [JsonPropertyOrder(2)]
    public IDictionary<string, object?> MetaData { get; set; } = new Dictionary<string, object?>();

    public static Message User(params IPart[] parts)
    {
        return new()
        {
            Role = "user",
            Parts = parts
        };
    }

    public static Message Agent(params IPart[] parts)
    {
        return new()
        {
            Role = "agent",
            Parts = parts
        };
    }
}