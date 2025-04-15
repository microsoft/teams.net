using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Tabs;

/// <summary>
/// Current tab request context, i.e., the current theme.
/// </summary>
public class Context
{
    /// <summary>
    /// The current user's theme.
    /// </summary>
    [JsonPropertyName("theme")]
    [JsonPropertyOrder(0)]
    public string? Theme { get; set; }
}