using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Search;

/// <summary>
/// Defines the structure that is returned as the result of an Invoke activity with
/// Name of 'application/search'.
/// </summary>
public class SearchResponse()
{
    /// <summary>
    /// The response status code.
    /// </summary>
    [JsonPropertyName("statusCode")]
    [JsonPropertyOrder(0)]
    public int StatusCode { get; set; } = 200;

    /// <summary>
    /// The type of this response.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(1)]
    public ContentType Type { get; } = ContentType.SearchResponse;

    /// <summary>
    /// the response value
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(2)]
    public object? Value { get; set; }
}