using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Apps.Extensions;

public class TeamsSettings
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? TenantId { get; set; }

    public IAppOptions Apply(IAppOptions? options = null)
    {
        options ??= new AppOptions();

        if (ClientId != null && ClientSecret != null)
        {
            options.Credentials = new ClientCredentials(ClientId, ClientSecret, TenantId);
        }

        return options;
    }
}