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

    public SuggestedActions AddRecipients(params string[] recipients)
    {
        foreach (var to in recipients)
        {
            To.Add(to);
        }

        return this;
    }

    public SuggestedActions AddAction(Cards.Action action)
    {
        Actions.Add(action);
        return this;
    }

    public SuggestedActions AddActions(params Cards.Action[] actions)
    {
        foreach (var action in actions)
        {
            Actions.Add(action);
        }

        return this;
    }
}