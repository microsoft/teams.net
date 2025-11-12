using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Apps.Extensions;

namespace Microsoft.Teams.Plugins.Agents;

public static partial class Extensions
{
    /// <summary>
    /// Adds the TeamsAgentPlugin
    /// </summary>
    public static IHostApplicationBuilder AddTeamsAgentPlugin(this IHostApplicationBuilder builder, TeamsAgentPluginOptions? options = null)
    {
        builder.Services.AddSingleton<TurnContextAccessor>();
        builder.Services.AddTeamsPlugin(sp =>
        {
            options ??= sp.GetService<TeamsAgentPluginOptions>() ?? new TeamsAgentPluginOptions();
            options.Provider ??= sp;
            return new TeamsAgentPlugin(options);
        });

        return builder;
    }
}