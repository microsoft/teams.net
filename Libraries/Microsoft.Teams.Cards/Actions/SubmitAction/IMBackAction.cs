using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class SubmitActionType : StringEnum
{
    public static readonly SubmitActionType IMBack = new("imBack");
    public bool IsIMBack => IMBack.Equals(Value);
}

public class IMBackAction : SubmitAction
{
    /// <summary>
    /// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonPropertyOrder(11)]
    public new IMBackActionData Data { get; set; }

    public IMBackAction(string value)
    {
        Data = new()
        {
            MSTeams = new(value)
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
    [JsonPropertyOrder(0)]
    public new required IMBackMSTeamsActionData MSTeams { get; set; }
}

/// <summary>
/// the IMBackAction teams data
/// </summary>
public class IMBackMSTeamsActionData(string value) : MSTeamsActionData(SubmitActionType.IMBack)
{
    /// <summary>
    /// String that needs to be echoed back in the chat.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(1)]
    public string Value { get; set; } = value;
}