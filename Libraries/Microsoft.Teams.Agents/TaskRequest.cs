using Microsoft.Teams.Common.Json;

namespace Microsoft.Teams.Agents;

/// <summary>
/// represents a request to perform
/// some operation in another agent
/// </summary>
[TrueTypeJson<ITaskRequest>]
public interface ITaskRequest
{
    /// <summary>
    /// the unique task name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// the task arguments
    /// </summary>
    public IList<object?> Arguments { get; }
}

/// <summary>
/// represents a request to perform
/// some operation in another agent
/// </summary>
public class TaskRequest : ITaskRequest
{
    public string Name { get; set; }
    public IList<object?> Arguments { get; set; }

    public TaskRequest(string name)
    {
        Name = name;
        Arguments = [];
    }

    public TaskRequest(string name, params object?[] arguments)
    {
        Name = name;
        Arguments = arguments;
    }
}