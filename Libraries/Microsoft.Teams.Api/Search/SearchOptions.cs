using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Search;

/// <summary>
/// Defines the query options for Invoke activity with Name of 'application/search'.
/// </summary>
public class SearchOptions
{
    /// <summary>
    /// Gets or sets the the starting reference number from which ordered search results should be returned.
    /// </summary>
    /// <value>
    /// The the starting reference number from which ordered search results should be returned.
    /// </value>
    [JsonPropertyName("skip")]
    [JsonPropertyOrder(0)]
    public int? Skip { get; set; }

    /// <summary>
    /// Gets or sets the number of search results that should be returned.
    /// </summary>
    /// <value>
    /// The number of search results that should be returned.
    /// </value>
    [JsonPropertyName("top")]
    [JsonPropertyOrder(1)]
    public int? Top { get; set; }
}