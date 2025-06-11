using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Search;

/// <summary>
/// Defines the structure that arrives in the Activity.Value for Invoke activity
/// with Name of 'application/search'.
/// </summary>
public class SearchValue
{
    /// <summary>
    /// Gets or sets the kind for the search invoke value.
    /// Must be either search, searchAnswer, or typeahead.
    /// </summary>
    /// <value>
    /// The kind for this search invoke action value.
    /// </value>
    [JsonPropertyName("kind")]
    [JsonPropertyOrder(0)]
    public required SearchType Kind { get; set; }

    /// <summary>
    /// Gets or sets the query text for the search invoke value.
    /// </summary>
    /// <value>
    /// The query text of this search invoke action value.
    /// </value>
    [JsonPropertyName("queryText")]
    [JsonPropertyOrder(1)]
    public required string QueryText { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="SearchOptions"/> for this search invoke.
    /// </summary>
    /// <value>
    /// The <see cref="SearchOptions"/> for this search invoke.
    /// </value>
    [JsonPropertyName("queryOptions")]
    [JsonPropertyOrder(2)]
    public SearchOptions? QueryOptions { get; set; }

    /// <summary>
    /// Gets or sets the context information about the query. Such as the UI
    /// control that issued the query. The type of the context field is object
    /// and is dependent on the kind field. For search and searchAnswers,
    /// there is no defined context value.
    /// </summary>
    /// <value>
    /// The context information about the query.
    /// </value>
    [JsonPropertyName("context")]
    [JsonPropertyOrder(3)]
    public object? Context { get; set; }
}