using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api;

public class SuggestedActions
{
    /// <summary>
    /// Ids of the recipients that the actions should be shown to.  These Ids are relative to the
    /// channelId and a subset of all recipients of the activity
    /// </summary>
    [JsonPropertyName("to")]
    [JsonPropertyOrder(0)]
    public IList<string> To { get; set; } = [];

    /// <summary>
    /// Actions that can be shown to the user
    /// </summary>
    [JsonPropertyName("actions")]
    [JsonPropertyOrder(1)]
    public IList<Cards.Action> Actions { get; set; } = [];
}