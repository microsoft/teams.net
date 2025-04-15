using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api;

/// <summary>
/// Tenant
/// </summary>
public class Tenant
{
    /// <summary>
    /// Unique identifier representing a tenant
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }
}