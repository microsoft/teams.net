// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.Handlers;

/// <summary>
/// Defines the structure returned as the result of an Invoke activity with Name of
/// 'application/search'.
/// </summary>
public class SearchResponse
{
    /// <summary>
    /// The response status code.
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; } = 200;

    /// <summary>
    /// The type of this response.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; } = "application/vnd.microsoft.search.searchResponse";

    /// <summary>
    /// The response value.
    /// </summary>
    [JsonPropertyName("value")]
    public SearchResponseValue? Value { get; set; }
}

/// <summary>
/// The value payload of a <see cref="SearchResponse"/>.
/// </summary>
public class SearchResponseValue
{
    /// <summary>
    /// The list of search results.
    /// </summary>
    [JsonPropertyName("results")]
    public IList<SearchResult> Results { get; set; } = [];
}

/// <summary>
/// A single result returned in a <see cref="SearchResponse"/>. For Adaptive Card dynamic
/// typeahead 'Input.ChoiceSet', 'Title' is the display text and 'Value' is the submitted value.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// The display text of the result.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// The value submitted when the result is selected.
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
