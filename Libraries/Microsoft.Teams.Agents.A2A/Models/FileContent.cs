using System.Text.Json.Serialization;

namespace Microsoft.Teams.Agents.A2A.Models;

/// <summary>
/// Represents the content of a file, either as base64 encoded bytes or a URI.\n\nEnsures that either 'bytes' or 'uri' is provided, but not both.
/// </summary>
public class FileContent
{
    [JsonPropertyName("name")]
    [JsonPropertyOrder(0)]
    public string? Name { get; set; }

    [JsonPropertyName("mimeType")]
    [JsonPropertyOrder(1)]
    public string? MimeType { get; set; }

    [JsonPropertyName("bytes")]
    [JsonPropertyOrder(2)]
    public string? Bytes { get; set; }

    [JsonPropertyName("uri")]
    [JsonPropertyOrder(3)]
    public string? Uri { get; set; }
}