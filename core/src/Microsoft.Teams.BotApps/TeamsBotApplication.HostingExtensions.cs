// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Teams.BotApps;

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
        ServiceProvider sp = services.BuildServiceProvider();
        IConfiguration configuration = sp.GetRequiredService<IConfiguration>();

        string scope = "https://api.botframework.com/.default";
        if (!string.IsNullOrEmpty(configuration[$"{sectionName}:Scope"]))
            scope = configuration[$"{sectionName}:Scope"]!;
        if (!string.IsNullOrEmpty(configuration["Scope"]))
            scope = configuration["Scope"]!;

        services.AddHttpClient<TeamsApiClient>(TeamsApiClient.TeamsHttpClientName)
            .AddHttpMessageHandler(sp =>
                new BotAuthenticationHandler(
                    sp.GetRequiredService<IAuthorizationHeaderProvider>(),
                    sp.GetRequiredService<ILogger<BotAuthenticationHandler>>(),
                    scope,
                    sp.GetService<IOptions<ManagedIdentityOptions>>()));

        services.AddBotApplication<TeamsBotApplication>();
        return services;
    }
}
