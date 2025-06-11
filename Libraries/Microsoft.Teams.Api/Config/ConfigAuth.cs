using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Config;

/// <summary>
/// Specifies bot config auth, including type and suggestedActions.
/// </summary>
public class ConfigAuth
{
    /// <summary>
    /// Gets or sets type of bot config auth.
    /// </summary>
    /// <value>
    /// The type of bot config auth.
    /// </value>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public string Type { get; set; } = "auth";

    /// <summary>
    /// Gets or sets suggested actions. 
    /// </summary>
    /// <value>
    /// The suggested actions of bot config auth.
    /// </value>
    [JsonPropertyName("suggestedActions")]
    [JsonPropertyOrder(1)]
    public SuggestedActions? SuggestedActions { get; set; }
}