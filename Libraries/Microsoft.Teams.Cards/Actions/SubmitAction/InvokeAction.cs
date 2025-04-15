using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class SubmitActionType : StringEnum
{
    public static readonly SubmitActionType Invoke = new("invoke");
    public bool IsInvoke => Invoke.Equals(Value);
}

public class InvokeAction : SubmitAction
{
    /// <summary>
    /// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonPropertyOrder(11)]
    public new InvokeActionData Data { get; set; }

    public InvokeAction(object? value)
    {
        Data = new(value)
        {
            MSTeams = new(value)
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
    [JsonPropertyOrder(0)]
    public new InvokeMSTeamsActionData MSTeams { get; set; } = new(value);
}

/// <summary>
/// the InvokeAction teams data
/// </summary>
public class InvokeMSTeamsActionData(object? value) : MSTeamsActionData(SubmitActionType.Invoke)
{
    /// <summary>
    /// Set the value to send with the invoke
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(1)]
    public object? Value { get; set; } = value;
}