using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.AI.Messages;

public class DeveloperMessage : IMessage
{
    [JsonPropertyName("role")]
    [JsonPropertyOrder(0)]
    public Role Role => Role.Developer;

    [JsonPropertyName("content")]
    [JsonPropertyOrder(1)]
    public string Content { get; set; }

    [JsonConstructor]
    public DeveloperMessage(string content)
    {
        Content = content;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}