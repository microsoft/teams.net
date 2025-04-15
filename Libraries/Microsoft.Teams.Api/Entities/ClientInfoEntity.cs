using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Entities;

public class ClientInfoEntity : Entity
{
    [JsonPropertyName("locale")]
    [JsonPropertyOrder(3)]
    public string? Locale { get; set; }

    [JsonPropertyName("country")]
    [JsonPropertyOrder(4)]
    public string? Country { get; set; }

    [JsonPropertyName("platform")]
    [JsonPropertyOrder(5)]
    public string? Platform { get; set; }

    [JsonPropertyName("timezone")]
    [JsonPropertyOrder(6)]
    public string? Timezone { get; set; }

    public ClientInfoEntity() : base("clientInfo") { }
}