using System.Text.Json.Serialization;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools.Models;

/// <summary>
/// the custom page that can be added
/// to the devtools
/// </summary>
public class Page
{
    /// <summary>
    /// an optional icon name
    /// to be shown in the view header
    /// </summary>
    [JsonPropertyName("icon")]
    [JsonPropertyOrder(0)]
    public string? Icon { get; set; }

    /// <summary>
    /// the unique name of the view
    /// (must be url safe)
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(1)]
    public string Name { get; set; }

    /// <summary>
    /// the display name of the view
    /// to be shown in the view header
    /// </summary>
    [JsonPropertyName("displayName")]
    [JsonPropertyOrder(2)]
    public string DisplayName { get; set; }

    /// <summary>
    /// the url of the view
    /// </summary>
    [JsonPropertyName("url")]
    [JsonPropertyOrder(3)]
    public string Url { get; set; }

    public Page(string name, string url)
    {
        Name = name;
        DisplayName = name;
        Url = url;
    }

    public Page(string name, string displayName, string url)
    {
        Name = name;
        DisplayName = displayName;
        Url = url;
    }
}