using System.Text.Json.Serialization;

namespace Microsoft.Teams.Cards;

public class IMBackAction : SubmitAction
{
    /// <summary>
    /// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
    /// </summary>
    [JsonPropertyName("data")]
    public new IMBackActionData Data { get; set; }

    public IMBackAction(string value)
    {
        Data = new()
        {
            MsTeams = new(value)
        };
    }
}

/// <summary>
/// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
/// </summary>
public class IMBackActionData : SubmitActionData
{
    /// <summary>
    /// Teams specific payload data.
    /// </summary>
    [JsonPropertyName("msteams")]
    public new required IMBackMSTeamsActionData MsTeams { get; set; }
}

/// <summary>
/// the IMBackAction teams data
/// </summary>
public class IMBackMSTeamsActionData(string value) : MsTeamsSubmitActionData
{
    /// <summary>
    /// The Teams-specifc sub-type of the action.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; } = "imBack";

    /// <summary>
    /// String that needs to be echoed back in the chat.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = value;
}