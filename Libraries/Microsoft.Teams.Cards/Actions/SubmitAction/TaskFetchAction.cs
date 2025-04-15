using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class SubmitActionType : StringEnum
{
    public static readonly SubmitActionType TaskFetch = new("task/fetch");
    public bool IsTaskFetch => TaskFetch.Equals(Value);
}

public class TaskFetchAction : SubmitAction
{
    /// <summary>
    /// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonPropertyOrder(11)]
    public new TaskFetchActionData Data { get; set; }

    public TaskFetchAction(object? value)
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
public class TaskFetchActionData : SubmitActionData
{
    /// <summary>
    /// Teams specific payload data.
    /// </summary>
    [JsonPropertyName("msteams")]
    [JsonPropertyOrder(0)]
    public new required TaskFetchMSTeamsActionData MSTeams { get; set; }
}

/// <summary>
/// the TaskFetchAction teams data
/// </summary>
public class TaskFetchMSTeamsActionData(object? value) : MSTeamsActionData(SubmitActionType.TaskFetch)
{
    /// <summary>
    /// The data value sent with the `task/fetch` invoke.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(1)]
    public object? Value { get; set; } = value;
}