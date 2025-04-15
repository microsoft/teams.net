namespace Microsoft.Teams.Agents.A2A.Models;

public enum TaskState : int
{
    Submitted,
    Working,
    InputRequired,
    Completed,
    Cancelled,
    Failed,
    Unknown
}