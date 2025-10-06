
using Microsoft.Agents.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Extensions;

namespace Microsoft.Teams.Plugins.Agents;

public static partial class HostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the TeamsAgentPlugin
    /// </summary>
    public static IHostApplicationBuilder AddTeamsAgentPlugin(this IHostApplicationBuilder builder, TeamsAgentPluginOptions? options = null)
    {
        builder.Services.AddSingleton<ITurnContext.Accessor>();
        builder.Services.AddTeamsPlugin(sp =>
        {
            var adapter = sp.GetRequiredService<IChannelAdapter>();
            var logger = sp.GetRequiredService<ILogger<TeamsAgentMiddleware>>();
            options ??= sp.GetService<TeamsAgentPluginOptions>() ?? new TeamsAgentPluginOptions();
            options.Provider ??= sp;

            var plugin = new TeamsAgentPlugin(options);
            adapter.Use(new TeamsAgentMiddleware(logger, plugin));
            return plugin;
        });

        return builder;
    }
}