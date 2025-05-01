using Microsoft.AspNetCore.Builder;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Plugins.AspNetCore;

public interface IAspNetCorePlugin : IPlugin
{
    public IApplicationBuilder Configure(IApplicationBuilder builder);
}