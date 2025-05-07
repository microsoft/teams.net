using System.Text.Json.Serialization;

namespace Microsoft.Teams.Cards;

public class InvokeAction : SubmitAction
{
    /// <summary>
    /// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
    /// </summary>
    [JsonPropertyName("data")]
    public new InvokeActionData Data { get; set; }

    public InvokeAction(object? value)
    {
        Data = new(value)
        {
            MsTeams = new(value)
        };
    }
}

/// <summary>
/// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
/// </summary>
public class InvokeActionData(object? value) : SubmitActionData
{
    /// <summary>
    /// Teams specific payload data.
    /// </summary>
    [JsonPropertyName("msteams")]
    public new InvokeMSTeamsActionData MsTeams { get; set; } = new(value);
}

/// <summary>
/// the InvokeAction teams data
/// </summary>
public class InvokeMSTeamsActionData(object? value) : MsTeamsSubmitActionData
{
    /// <summary>
    /// The Teams-specifc sub-type of the action.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; } = "invoke";

    /// <summary>
    /// Set the value to send with the invoke
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; } = value;
}