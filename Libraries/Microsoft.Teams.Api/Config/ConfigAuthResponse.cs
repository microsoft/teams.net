namespace Microsoft.Teams.Api.Config;

/// <summary>
/// Envelope for Config Task Response.
/// </summary>
public class ConfigAuthResponse(ConfigAuth auth) : ConfigResponse<ConfigAuth>(auth)
{

}