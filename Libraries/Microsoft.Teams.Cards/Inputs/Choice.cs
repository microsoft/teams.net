using System.Text.Json.Serialization;

namespace Microsoft.Teams.Cards;

/// <summary>
/// Describes a choice for use in a ChoiceSet.
/// </summary>
public class Choice
{
    /// <summary>
    /// Text to display.
    /// </summary>
    [JsonPropertyName("title")]
    [JsonPropertyOrder(0)]
    public required string Title { get; set; }

    /// <summary>
    /// The raw value for the choice. NOTE: do not use a `,` in the value, since a `ChoiceSet` with `isMultiSelect` set to `true` returns a comma-delimited string of choice values.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(1)]
    public required string Value { get; set; }
}

/// <summary>
/// The data populated in the event payload for fetching dynamic choices, sent to the card-author to help identify the dataset from which choices might be fetched to be displayed in the dropdown. It might contain auxillary data to limit the maximum number of choices that can be sent and to support pagination.
/// </summary>
public class ChoiceDataQuery
{
    /// <summary>
    /// The dataset to be queried to get the choices.
    /// </summary>
    [JsonPropertyName("dataset")]
    [JsonPropertyOrder(0)]
    public required string DataSet { get; set; }

    /// <summary>
    /// The maximum number of choices that should be returned by the query. It can be ignored if the card-author wants to send a different number.
    /// </summary>
    [JsonPropertyName("count")]
    [JsonPropertyOrder(1)]
    public int? Count { get; set; }

    /// <summary>
    /// The number of choices to be skipped in the list of choices returned by the query. It can be ignored if the card-author does not want pagination.
    /// </summary>
    [JsonPropertyName("skip")]
    [JsonPropertyOrder(2)]
    public int? Skip { get; set; }
}