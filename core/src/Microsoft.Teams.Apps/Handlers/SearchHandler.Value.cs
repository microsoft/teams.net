// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.Handlers;

/// <summary>
/// Defines the structure that arrives in the Activity.Value for an Invoke activity with
/// Name of 'application/search'. Sent by Adaptive Card dynamic typeahead 'Input.ChoiceSet'
/// inputs (via 'choices.data' / 'Data.Query').
/// </summary>
public class SearchValue
{
    /// <summary>
    /// The kind for this search invoke value. Must be either 'search', 'searchAnswer', or 'typeahead'.
    /// </summary>
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    /// <summary>
    /// The query text of this search invoke value.
    /// </summary>
    [JsonPropertyName("queryText")]
    public string? QueryText { get; set; }

    /// <summary>
    /// The query options for this search invoke.
    /// </summary>
    [JsonPropertyName("queryOptions")]
    public SearchOptions? QueryOptions { get; set; }

    /// <summary>
    /// Context information about the query, such as the UI control that issued the query.
    /// </summary>
    [JsonPropertyName("context")]
    public object? Context { get; set; }

    /// <summary>
    /// The identifier of the dataset from which to fetch the choices, as authored on the
    /// Adaptive Card 'Data.Query'.
    /// </summary>
    [JsonPropertyName("dataset")]
    public string? Dataset { get; set; }
}

/// <summary>
/// Defines the query options for an Invoke activity with Name of 'application/search'.
/// </summary>
public class SearchOptions
{
    /// <summary>
    /// The starting reference number from which ordered search results should be returned.
    /// </summary>
    [JsonPropertyName("skip")]
    public int? Skip { get; set; }

    /// <summary>
    /// The number of search results that should be returned.
    /// </summary>
    [JsonPropertyName("top")]
    public int? Top { get; set; }
}
