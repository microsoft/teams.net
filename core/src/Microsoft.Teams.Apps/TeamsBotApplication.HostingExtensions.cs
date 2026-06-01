// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Hosting;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Extension methods for <see cref="TeamsBotApplication"/>.
/// </summary>
public static class TeamsBotApplicationHostingExtensions
{
    /// <summary>
    /// Registers Teams bot application services using the <see cref="WebApplicationBuilder"/>.
    /// This is a convenience method that delegates to <c>builder.Services.AddTeams()</c>.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="sectionName">The configuration section name for AzureAd settings. Default is "AzureAd".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTeams(this WebApplicationBuilder builder, string sectionName = "AzureAd")
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Services.AddTeams(sectionName);
    }

    /// <summary>
    /// Registers Teams bot application services using the <see cref="WebApplicationBuilder"/> with an <see cref="AppBuilder"/>.
    /// This supports the <c>App.Builder().AddOAuth("graph")</c> pattern from the old library.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="appBuilder">The app builder containing configuration.</param>
    /// <param name="sectionName">The configuration section name for AzureAd settings. Default is "AzureAd".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTeams(this WebApplicationBuilder builder, AppBuilder appBuilder, string sectionName = "AzureAd")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(appBuilder);
        return builder.Services.AddTeamsBotApplication(options =>
        {
            foreach (TeamsBotApplicationOptions.OAuthFlowDescriptor flow in appBuilder.Options.OAuthFlows)
            {
                options.AddOAuthFlow(flow.ConnectionName);
            }
        }, sectionName);
    }

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
    /// Adds the default <see cref="TeamsBotApplication"/> to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="sectionName">The configuration section name for AzureAd settings. Default is "AzureAd".</param>
    /// <returns>The service collection for chaining.</returns>
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
    /// Adds a custom <see cref="TeamsBotApplication"/> to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="sectionName">The configuration section name for AzureAd settings. Default is "AzureAd".</param>
    /// <returns>The service collection for chaining.</returns>
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

        // Register TeamsBotApplicationOptions
        TeamsBotApplicationOptions teamsOptions = new() { AppId = botConfig.ClientId };
        configure?.Invoke(teamsOptions);
        services.AddSingleton(teamsOptions);

        services.AddBotApplication<TApp>(botConfig);
        services.AddBotHttpClient(nameof(ApiClient), botConfig);
        services.AddSingleton(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient(nameof(ApiClient));
            var conversationClient = sp.GetRequiredService<ConversationClient>();
            var userTokenClient = sp.GetRequiredService<UserTokenClient>();
            return new ApiClient(httpClient, conversationClient, userTokenClient);
        });
        return services;
    }

    /// <summary>
    /// Configures a custom <typeparamref name="TApp"/> on the endpoint route builder.
    /// </summary>
    /// <typeparam name="TApp">The custom <see cref="TeamsBotApplication"/> type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="routePath">The route path to listen on. Default is "api/messages".</param>
    /// <returns>The configured <typeparamref name="TApp"/> instance.</returns>
    public static TApp UseTeamsBotApplication<TApp>(this IEndpointRouteBuilder endpoints,
       string routePath = "api/messages")
           where TApp : TeamsBotApplication
        => endpoints.UseBotApplication<TApp>(routePath);

    /// <summary>
    /// Configures the default <see cref="TeamsBotApplication"/> on the endpoint route builder.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="routePath">The route path to listen on. Default is "api/messages".</param>
    /// <returns>The configured <see cref="TeamsBotApplication"/> instance.</returns>
    public static TeamsBotApplication UseTeamsBotApplication(this IEndpointRouteBuilder endpoints,
       string routePath = "api/messages")
        => endpoints.UseBotApplication<TeamsBotApplication>(routePath);

    /// <summary>
    /// Configures the default <see cref="TeamsBotApplication"/>. Alias for <see cref="UseTeamsBotApplication(IEndpointRouteBuilder, string)"/>.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="routePath">The route path to listen on. Default is "api/messages".</param>
    /// <returns>The configured <see cref="TeamsBotApplication"/> instance.</returns>
    public static TeamsBotApplication UseTeams(this IEndpointRouteBuilder endpoints, string routePath = "api/messages")
        => endpoints.UseBotApplication<TeamsBotApplication>(routePath);
}
