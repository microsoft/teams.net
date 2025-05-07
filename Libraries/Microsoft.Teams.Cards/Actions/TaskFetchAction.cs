using System.Text.Json.Serialization;

namespace Microsoft.Teams.Cards;

public class TaskFetchAction : SubmitAction
{
    /// <summary>
    /// Initial data that input fields will be combined with. These are essentially ‘hidden’ properties.
    /// </summary>
    [JsonPropertyName("data")]
    public new TaskFetchActionData Data { get; set; }

    public TaskFetchAction(object? value)
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
public class TaskFetchActionData : SubmitActionData
{
    /// <summary>
    /// Teams specific payload data.
    /// </summary>
    [JsonPropertyName("msteams")]
    public new required TaskFetchMSTeamsActionData MsTeams { get; set; }
}

/// <summary>
/// the TaskFetchAction teams data
/// </summary>
public class TaskFetchMSTeamsActionData(object? value) : MsTeamsSubmitActionData
{
    /// <summary>
    /// The Teams-specifc sub-type of the action.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; } = "task/fetch";

    /// <summary>
    /// The data value sent with the `task/fetch` invoke.
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; } = value;
}