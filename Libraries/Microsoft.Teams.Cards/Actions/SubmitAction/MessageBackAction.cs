using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class SubmitActionType : StringEnum
{
    public static readonly SubmitActionType MessageBack = new("messageBack");
    public bool IsMessageBack => MessageBack.Equals(Value);
}

public class MessageBackAction : SubmitAction
{
    /// <summary>
    /// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonPropertyOrder(11)]
    public new MessageBackActionData Data { get; set; }

    public MessageBackAction(string text, string value)
    {
        Data = new()
        {
            MSTeams = new()
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
            MSTeams = new()
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
    [JsonPropertyOrder(0)]
    public new required MessageBackMSTeamsActionData MSTeams { get; set; }
}

/// <summary>
/// the MessageBackAction teams data
/// </summary>
public class MessageBackMSTeamsActionData() : MSTeamsActionData(SubmitActionType.MessageBack)
{
    /// <summary>
    /// Sent to your bot when the action is performed.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonPropertyOrder(1)]
    public required string Text { get; set; }

    /// <summary>
    /// Used by the user in the chat stream when the action is performed.
    /// This text isn't sent to your bot.
    /// </summary>
    [JsonPropertyName("displayText")]
    [JsonPropertyOrder(2)]
    public string? DisplayText { get; set; }

    /// <summary>
    /// Sent to your bot when the action is performed. You can encode context
    /// for the action, such as unique identifiers or a `JSON` object.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(3)]
    public required string Value { get; set; }
}