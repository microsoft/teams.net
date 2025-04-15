using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.MessageExtensions;

/// <summary>
/// Messaging extension query parameters
/// </summary>
public class Parameter
{
    /// <summary>
    /// Name of the parameter
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(0)]
    public string? Name { get; set; }

    /// <summary>
    /// Value of the parameter
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(1)]
    public object? Value { get; set; }
}