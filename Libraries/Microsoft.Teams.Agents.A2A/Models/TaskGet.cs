using System.Text.Json.Serialization;

using Microsoft.Teams.Agents.A2A.Json.Rpc;

namespace Microsoft.Teams.Agents.A2A.Models;

public static partial class Requests
{
    public static Request GetTask(TaskGet data) => new()
    {
        Method = "tasks/get",
        Params = data
    };
}

/// <summary>
/// Clients may use this method to retrieve the generated Artifacts for a Task. The agent determines the retention window for Tasks previously submitted to it.
/// An agent may return an error code for Tasks that were past the retention window for an agent or for Tasks that are short-lived and not persisted by the agent.
/// The client may also request the last N items of history of the Task which will include all Messages, in order, sent by client and server. By default this is 0 (no history).
/// https://google.github.io/A2A/#/documentation?id=get-a-task
/// </summary>
public class TaskGet
{
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("historyLength")]
    [JsonPropertyOrder(2)]
    public int? HistoryLength { get; set; }

    [JsonPropertyName("metadata")]
    [JsonPropertyOrder(3)]
    public IDictionary<string, object?> MetaData { get; set; } = new Dictionary<string, object?>();
}