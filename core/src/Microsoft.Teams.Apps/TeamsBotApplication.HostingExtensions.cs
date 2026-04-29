// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Core.Hosting;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Extension methods for <see cref="TeamsBotApplication"/>.
/// </summary>
public static class TeamsBotApplicationHostingExtensions
{
    /// <summary>
    /// Registers Teams bot application services with the specified service collection.
    /// </summary>
    /// <remarks>This method provides a simplified way to configure Teams bot support by encapsulating the
    /// necessary service registrations and configuration binding.</remarks>
    /// <param name="services">The service collection to which Teams bot application services will be added. Cannot be null.</param>
    /// <param name="sectionName">The name of the configuration section containing Azure Active Directory settings. Defaults to "AzureAd" if not
    /// specified.</param>
    /// <returns>The service collection with Teams bot application services registered.</returns>
    public static IServiceCollection AddTeams(this IServiceCollection services, string sectionName = "AzureAd")
        => AddTeamsBotApplication(services, sectionName);

    /// <summary>
    /// Adds the Default TeamsBotApplication
    /// </summary>
    /// <param name="services"></param>
    /// <param name="sectionName"></param>
    /// <returns></returns>
    public static IServiceCollection AddTeamsBotApplication(this IServiceCollection services, string sectionName = "AzureAd")
    {
        return AddTeamsBotApplication<TeamsBotApplication>(services, sectionName);
    }

    /// <summary>
    /// Adds the default TeamsBotApplication with configuration options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">A delegate to configure <see cref="TeamsBotApplicationOptions"/>.</param>
    /// <param name="sectionName">The configuration section name for AzureAd settings. Default is "AzureAd".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTeamsBotApplication(this IServiceCollection services, Action<TeamsBotApplicationOptions> configure, string sectionName = "AzureAd")
    {
        return AddTeamsBotApplication<TeamsBotApplication>(services, configure, sectionName);
    }

    /// <summary>
    /// Adds a custom TeamsBotApplication
    /// </summary>
    /// <param name="services">The WebApplicationBuilder instance.</param>
    /// <param name="sectionName">The configuration section name for AzureAd settings. Default is "AzureAd".</param>
    /// <returns>The updated WebApplicationBuilder instance.</returns>
    public static IServiceCollection AddTeamsBotApplication<TApp>(this IServiceCollection services, string sectionName = "AzureAd") where TApp : TeamsBotApplication
    {
        return AddTeamsBotApplication<TApp>(services, configure: null, sectionName);
    }

    /// <summary>
    /// Adds a custom TeamsBotApplication with configuration options.
    /// </summary>
    /// <typeparam name="TApp">The custom TeamsBotApplication type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">A delegate to configure <see cref="TeamsBotApplicationOptions"/>. Can be null.</param>
    /// <param name="sectionName">The configuration section name for AzureAd settings. Default is "AzureAd".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTeamsBotApplication<TApp>(this IServiceCollection services, Action<TeamsBotApplicationOptions>? configure, string sectionName = "AzureAd") where TApp : TeamsBotApplication
    {
        BotConfig botConfig = BotConfig.Resolve(services, sectionName);

        services.AddBotClient<ApiClient>(nameof(ApiClient), botConfig);

        // Register TeamsBotApplicationOptions
        TeamsBotApplicationOptions teamsOptions = new();
        configure?.Invoke(teamsOptions);
        services.AddSingleton(teamsOptions);

        services.AddBotApplication<TApp>(botConfig);
        return services;
    }

    /// <summary>
    /// Configures the TeamsBotApp 
    /// </summary>
    /// <typeparam name="TApp"></typeparam>
    /// <param name="endpoints"></param>
    /// <param name="routePath"></param>
    /// <returns></returns>
    public static TApp UseTeamsBotApplication<TApp>(this IEndpointRouteBuilder endpoints,
       string routePath = "api/messages")
           where TApp : TeamsBotApplication
        => endpoints.UseBotApplication<TApp>(routePath);

    /// <summary>
    /// Configures the default TeamsBotApplication
    /// </summary>
    /// <param name="endpoints"></param>
    /// <param name="routePath"></param>
    /// <returns></returns>
    public static TeamsBotApplication UseTeamsBotApplication(this IEndpointRouteBuilder endpoints,
       string routePath = "api/messages")
        => endpoints.UseBotApplication<TeamsBotApplication>(routePath);

    /// <summary>
    /// Alias for backward compat
    /// </summary>
    /// <param name="endpoints"></param>
    /// <param name="routePath"></param>
    /// <returns></returns>
    public static TeamsBotApplication UseTeams(this IEndpointRouteBuilder endpoints, string routePath = "api/messages")
        => endpoints.UseBotApplication<TeamsBotApplication>(routePath);
}
