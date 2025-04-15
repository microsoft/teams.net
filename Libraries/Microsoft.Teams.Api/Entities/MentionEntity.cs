using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Entities;

public class MentionEntity : Entity
{
    [JsonPropertyName("mentioned")]
    [JsonPropertyOrder(3)]
    public required Account Mentioned { get; set; }

    [JsonPropertyName("text")]
    [JsonPropertyOrder(4)]
    public string? Text { get; set; }

    public MentionEntity() : base("mention") { }
}