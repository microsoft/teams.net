namespace Microsoft.Teams.Api.Config;

/// <summary>
/// Envelope for Config Task Response.
/// </summary>
public class ConfigTaskResponse(TaskModules.Task task) : ConfigResponse<TaskModules.Task>(task)
{

}