using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Token;

/// <summary>
/// An interface representing TokenPostResource.
/// </summary>
public class PostResource
{
    [JsonPropertyName("sasUrl")]
    [JsonPropertyOrder(0)]
    public string? SasUrl { get; set; }
}