using System.Text.Json.Serialization;

namespace Microsoft.Teams.Cards;

public class MessageBackAction : SubmitAction
{
    /// <summary>
    /// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
    /// </summary>
    [JsonPropertyName("data")]
    public new MessageBackActionData Data { get; set; }

    public MessageBackAction(string text, string value)
    {
        Data = new()
        {
            MsTeams = new()
            {
                Text = text,
                Value = value
            }
        };
    }

    public MessageBackAction(string text, string displayText, string value)
    {
        Data = new()
        {
            MsTeams = new()
            {
                Text = text,
                DisplayText = displayText,
                Value = value
            }
        };
    }
}

/// <summary>
/// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
/// </summary>
public class MessageBackActionData : SubmitActionData
{
    /// <summary>
    /// Teams specific payload data.
    /// </summary>
    [JsonPropertyName("msteams")]
    public new required MessageBackMSTeamsActionData MsTeams { get; set; }
}

/// <summary>
/// the MessageBackAction teams data
/// </summary>
public class MessageBackMSTeamsActionData() : MsTeamsSubmitActionData
{
    /// <summary>
    /// The Teams-specifc sub-type of the action.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; } = "messageBack";

    /// <summary>
    /// Sent to your bot when the action is performed.
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; set; }

    /// <summary>
    /// Used by the user in the chat stream when the action is performed.
    /// This text isn't sent to your bot.
    /// </summary>
    [JsonPropertyName("displayText")]
    public string? DisplayText { get; set; }

    /// <summary>
    /// Sent to your bot when the action is performed. You can encode context
    /// for the action, such as unique identifiers or a `JSON` object.
    /// </summary>
    [JsonPropertyName("value")]
    public required string Value { get; set; }
}