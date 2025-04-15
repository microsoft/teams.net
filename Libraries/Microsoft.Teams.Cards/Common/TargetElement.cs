using System.Text.Json.Serialization;

namespace Microsoft.Teams.Cards;

/// <summary>
/// Represents an entry for Action.ToggleVisibility's targetElements property
/// </summary>
public class TargetElement
{
    /// <summary>
    /// the type
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public string Type { get; } = "TargetElement";

    /// <summary>
    /// Element ID of element to toggle
    /// </summary>
    [JsonPropertyName("elementId")]
    [JsonPropertyOrder(1)]
    public required string ElementId { get; set; }

    /// <summary>
    /// If `true`, always show target element. If `false`, always hide target element. If not supplied, toggle target element's visibility.
    /// </summary>
    [JsonPropertyName("isVisible")]
    [JsonPropertyOrder(2)]
    public bool? IsVisible { get; set; }
}