// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Core.Hosting;

namespace Microsoft.Teams.Bot.Apps;

/// <summary>
/// Extension methods for <see cref="TeamsBotApplication"/>.
/// </summary>
public static class TeamsBotApplicationHostingExtensions
{
    /// <summary>
    /// Adds TeamsBotApplication to the service collection.
    /// </summary>
    /// <param name="services">The WebApplicationBuilder instance.</param>
    /// <param name="sectionName">The configuration section name for AzureAd settings. Default is "AzureAd".</param>
    /// <returns>The updated WebApplicationBuilder instance.</returns>
    public static IServiceCollection AddTeamsBotApplication(this IServiceCollection services, string sectionName = "AzureAd")
    {
        // Register options to defer configuration reading until ServiceProvider is built
        services.AddOptions<BotClientOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                options.Scope = "https://api.botframework.com/.default";
                if (!string.IsNullOrEmpty(configuration[$"{sectionName}:Scope"]))
                    options.Scope = configuration[$"{sectionName}:Scope"]!;
                if (!string.IsNullOrEmpty(configuration["Scope"]))
                    options.Scope = configuration["Scope"]!;
                options.SectionName = sectionName;
            });

        services.AddHttpClient<TeamsApiClient>(TeamsApiClient.TeamsHttpClientName)
            .AddHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptions<BotClientOptions>>().Value;
                return new BotAuthenticationHandler(
                    sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                    sp.GetRequiredService<ILogger<BotAuthenticationHandler>>(),
                    options.Scope,
                    sp.GetService<IOptions<ManagedIdentityOptions>>());
            });

       //services.AddSingleton<Router>();
        services.AddBotApplication<TeamsBotApplication>();
        return services;
    }
}
