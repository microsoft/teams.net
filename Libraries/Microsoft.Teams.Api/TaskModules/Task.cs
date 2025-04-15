using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.TaskModules;

/// <summary>
/// Task Types
/// </summary>
[JsonConverter(typeof(JsonConverter<TaskType>))]
public partial class TaskType(string value) : StringEnum(value)
{
}

/// <summary>
/// Base class for Task Module responses
/// </summary>
public abstract class Task(TaskType type)
{
    /// <summary>
    /// Choice of action options when responding to the
    /// task/submit message. Possible values include: 'message', 'continue'
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public TaskType Type { get; set; } = type;
}