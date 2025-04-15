using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Tabs;

/// <summary>
/// Tab SuggestedActions (Only when type is 'auth' or 'silentAuth').
/// </summary>
public class SuggestedActions
{
    /// <summary>
    /// Actions that can be shown to the user
    /// </summary>
    [JsonPropertyName("actions")]
    [JsonPropertyOrder(1)]
    public IList<Cards.Action> Actions { get; set; } = [];
}