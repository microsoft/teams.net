using System.Text.Json.Serialization;

namespace Samples.Tab;

public class Body
{
    [JsonPropertyName("message")]
    public required string Message { get; set; }
}