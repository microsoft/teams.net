// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.SocketMode;
using Microsoft.Teams.Apps.State;
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
    [Obsolete("AddTeams is a backward-compatibility shim for the old library and will be removed. Use AddTeamsBotApplication instead.")]
    public static IServiceCollection AddTeams(this WebApplicationBuilder builder, string sectionName = "AzureAd")
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Services.AddTeamsBotApplication(sectionName);
    }

    /// <summary>
    /// Registers Teams bot application services using the <see cref="WebApplicationBuilder"/> with an <see cref="AppBuilder"/>.
    /// This supports the <c>App.Builder().AddOAuth("graph")</c> pattern from the old library.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="appBuilder">The app builder containing configuration.</param>
    /// <param name="sectionName">The configuration section name for AzureAd settings. Default is "AzureAd".</param>
    /// <returns>The service collection for chaining.</returns>
    [Obsolete("The AppBuilder overload is a backward-compatibility shim for the old library's App.Builder() pattern and will be removed. Configure OAuth flows via DI instead: builder.Services.AddTeamsBotApplication(options => options.AddOAuthFlow(\"connectionName\")).")]
    public static IServiceCollection AddTeams(this WebApplicationBuilder builder, AppBuilder appBuilder, string sectionName = "AzureAd")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(appBuilder);
#pragma warning disable CS0618 // AppBuilder is obsolete; this overload exists solely to support it.
        IReadOnlyList<TeamsBotApplicationOptions.OAuthFlowDescriptor> flows = appBuilder.Options.OAuthFlows;
#pragma warning restore CS0618
        return builder.Services.AddTeamsBotApplication(options =>
        {
            foreach (TeamsBotApplicationOptions.OAuthFlowDescriptor flow in flows)
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
    [Obsolete("AddTeams is a backward-compatibility shim for the old library and will be removed. Use AddTeamsBotApplication instead.")]
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
    /// Registers the Teams <see cref="ApiClient"/> using a named <see cref="HttpClient"/>.
    /// </summary>
    /// <remarks>
    /// The calling host must register the named HTTP client, <see cref="ConversationClient"/>,
    /// and <see cref="UserTokenClient"/> before resolving the API client.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="httpClientName">The name of the registered HTTP client to use.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTeamsApiClient(this IServiceCollection services, string httpClientName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(httpClientName);

        services.AddSingleton(sp =>
        {
            IHttpClientFactory factory = sp.GetRequiredService<IHttpClientFactory>();
            HttpClient httpClient = factory.CreateClient(httpClientName);
            ConversationClient conversationClient = sp.GetRequiredService<ConversationClient>();
            UserTokenClient userTokenClient = sp.GetRequiredService<UserTokenClient>();
            return new ApiClient(httpClient, conversationClient, userTokenClient);
        });

        return services;
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
        // BotConfig has already validated this as an absolute URI, so the parse cannot fail here.
        TeamsBotApplicationOptions teamsOptions = new() { AppId = botConfig.ClientId, GraphBaseUrl = new Uri(botConfig.GraphBaseUrl) };
        configure?.Invoke(teamsOptions);
        services.AddSingleton(teamsOptions);

        services.AddBotApplication<TApp>(botConfig);

        if (teamsOptions.IsStateEnabled)
        {
            AddTeamsBotApplicationState(services, teamsOptions.StateConfiguration);
        }

        services.AddBotHttpClient(nameof(ApiClient), botConfig);

        // Typed client for file downloads. Deliberately separate from the ApiClient's: a file download URL embeds its
        // own `tempauth` credential, so the request must not carry bot credentials.
        services.AddHttpClient<Files.FileDownloader>();

        services.AddTeamsApiClient(nameof(ApiClient));

        // A custom subclass forwards only the constructor arguments it declares, and the shape this SDK documents declares four, so a service added to the base constructor later never reaches it.
        // Re-registering TApp to back-fill after construction keeps every documented subclass working unchanged.
        // Without it the whole Agentic User file path is silently unreachable from a subclass: every content-URL-only file reports no credential.
        services.AddSingleton<TApp>(sp =>
        {
            TApp app = ActivatorUtilities.CreateInstance<TApp>(sp);
            app.TokenProvider ??= sp.GetKeyedService<BotTokenProvider>(sectionName);
            return app;
        });

        if (teamsOptions.SocketMode is { } socketMode)
        {
            SocketModeServiceRegistration.AddSocketMode<TApp>(services, botConfig, socketMode);
        }

        return services;
    }

    /// <summary>
    /// Configures a custom <typeparamref name="TApp"/> to receive activities over HTTP on the endpoint route builder.
    /// </summary>
    /// <remarks>
    /// Use this overload to map the messaging endpoint on a route group or other endpoint builder. To receive
    /// activities over Socket Mode, enable it with <see cref="TeamsBotApplicationOptions.UseSocketMode(bool)"/> and call
    /// <see cref="UseTeamsBotApplication{TApp}(IHost)"/> on a host without a web server.
    /// </remarks>
    /// <typeparam name="TApp">The custom <see cref="TeamsBotApplication"/> type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="routePath">The route path to listen on. Default is "api/messages".</param>
    /// <returns>The configured <typeparamref name="TApp"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Socket Mode is enabled.</exception>
    public static TApp UseTeamsBotApplication<TApp>(this IEndpointRouteBuilder endpoints,
       string routePath = "api/messages")
           where TApp : TeamsBotApplication
        => UseHttp<TApp>(endpoints, routePath);

    /// <summary>
    /// Configures the default <see cref="TeamsBotApplication"/> to receive activities over HTTP on the endpoint route builder.
    /// </summary>
    /// <remarks>
    /// Use this overload to map the messaging endpoint on a route group or other endpoint builder. To receive
    /// activities over Socket Mode, enable it with <see cref="TeamsBotApplicationOptions.UseSocketMode(bool)"/> and call
    /// <see cref="UseTeamsBotApplication(IHost)"/> on a host without a web server.
    /// </remarks>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="routePath">The route path to listen on. Default is "api/messages".</param>
    /// <returns>The configured <see cref="TeamsBotApplication"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Socket Mode is enabled.</exception>
    public static TeamsBotApplication UseTeamsBotApplication(this IEndpointRouteBuilder endpoints,
       string routePath = "api/messages")
        => UseHttp<TeamsBotApplication>(endpoints, routePath);

    /// <summary>
    /// Configures a custom <typeparamref name="TApp"/> to receive activities over HTTP on a web application.
    /// </summary>
    /// <remarks>
    /// Receives activities at <paramref name="routePath"/>. This overload exists so that a <see cref="WebApplication"/>,
    /// which is both an <see cref="IHost"/> and an <see cref="IEndpointRouteBuilder"/>, binds unambiguously. Socket Mode
    /// runs without a web server; build a Socket Mode bot with <c>Host.CreateApplicationBuilder()</c> and call
    /// <see cref="UseTeamsBotApplication{TApp}(IHost)"/> instead.
    /// </remarks>
    /// <typeparam name="TApp">The custom <see cref="TeamsBotApplication"/> type.</typeparam>
    /// <param name="app">The web application.</param>
    /// <param name="routePath">The route path to listen on. Default is "api/messages".</param>
    /// <returns>The configured <typeparamref name="TApp"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Socket Mode is enabled with
    /// <see cref="TeamsBotApplicationOptions.UseSocketMode(bool)"/>.</exception>
    public static TApp UseTeamsBotApplication<TApp>(this WebApplication app,
       string routePath = "api/messages")
           where TApp : TeamsBotApplication
        => UseTeamsBotApplicationCore<TApp>(app, routePath);

    /// <summary>
    /// Configures the default <see cref="TeamsBotApplication"/> to receive activities over HTTP on a web application.
    /// </summary>
    /// <remarks>
    /// Receives activities at <paramref name="routePath"/>. This overload exists so that a <see cref="WebApplication"/>,
    /// which is both an <see cref="IHost"/> and an <see cref="IEndpointRouteBuilder"/>, binds unambiguously. Socket Mode
    /// runs without a web server; build a Socket Mode bot with <c>Host.CreateApplicationBuilder()</c> and call
    /// <see cref="UseTeamsBotApplication(IHost)"/> instead.
    /// </remarks>
    /// <param name="app">The web application.</param>
    /// <param name="routePath">The route path to listen on. Default is "api/messages".</param>
    /// <returns>The configured <see cref="TeamsBotApplication"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when Socket Mode is enabled with
    /// <see cref="TeamsBotApplicationOptions.UseSocketMode(bool)"/>.</exception>
    public static TeamsBotApplication UseTeamsBotApplication(this WebApplication app,
       string routePath = "api/messages")
        => UseTeamsBotApplicationCore<TeamsBotApplication>(app, routePath);

    /// <summary>
    /// Configures a custom <typeparamref name="TApp"/> on a host, using the transport chosen in
    /// <see cref="AddTeamsBotApplication{TApp}(IServiceCollection, Action{TeamsBotApplicationOptions}?, string)"/>.
    /// </summary>
    /// <remarks>
    /// When Socket Mode is enabled with <see cref="TeamsBotApplicationOptions.UseSocketMode(bool)"/>, the host must not
    /// include a web server, such as one built with <c>Host.CreateApplicationBuilder()</c>. The transport starts with the
    /// host, which does not finish starting until every geo is connected. Otherwise the host must also be an endpoint
    /// route builder, such as a <see cref="WebApplication"/>, and activities are received over HTTP at <c>api/messages</c>.
    /// </remarks>
    /// <typeparam name="TApp">The custom <see cref="TeamsBotApplication"/> type.</typeparam>
    /// <param name="host">The built host.</param>
    /// <returns>The registered <typeparamref name="TApp"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the host cannot use the chosen transport, or when the
    /// application is not registered.</exception>
    public static TApp UseTeamsBotApplication<TApp>(this IHost host)
        where TApp : TeamsBotApplication
        => UseTeamsBotApplicationCore<TApp>(host, "api/messages");

    /// <summary>
    /// Configures the default <see cref="TeamsBotApplication"/> on a host, using the transport chosen in
    /// <see cref="AddTeamsBotApplication(IServiceCollection, Action{TeamsBotApplicationOptions}, string)"/>.
    /// </summary>
    /// <remarks>
    /// When Socket Mode is enabled with <see cref="TeamsBotApplicationOptions.UseSocketMode(bool)"/>, the host must not
    /// include a web server, such as one built with <c>Host.CreateApplicationBuilder()</c>. The transport starts with the
    /// host, which does not finish starting until every geo is connected. Otherwise the host must also be an endpoint
    /// route builder, such as a <see cref="WebApplication"/>, and activities are received over HTTP at <c>api/messages</c>.
    /// </remarks>
    /// <param name="host">The built host.</param>
    /// <returns>The registered <see cref="TeamsBotApplication"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the host cannot use the chosen transport, or when the
    /// application is not registered.</exception>
    public static TeamsBotApplication UseTeamsBotApplication(this IHost host)
        => UseTeamsBotApplicationCore<TeamsBotApplication>(host, "api/messages");

    /// <summary>
    /// Configures the default <see cref="TeamsBotApplication"/>. Alias for <see cref="UseTeamsBotApplication(IEndpointRouteBuilder, string)"/>.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="routePath">The route path to listen on. Default is "api/messages".</param>
    /// <returns>The configured <see cref="TeamsBotApplication"/> instance.</returns>
    [Obsolete("UseTeams is a backward-compatibility shim for the old library and will be removed. Use UseTeamsBotApplication instead.")]
    public static TeamsBotApplication UseTeams(this IEndpointRouteBuilder endpoints, string routePath = "api/messages")
        => UseHttp<TeamsBotApplication>(endpoints, routePath);

    // UseSocketMode is the single switch: Socket Mode needs a host without a web server, and HTTP needs one.
    private static TApp UseTeamsBotApplicationCore<TApp>(IHost host, string routePath)
        where TApp : TeamsBotApplication
    {
        ArgumentNullException.ThrowIfNull(host);

        if (IsSocketModeEnabled(host.Services))
        {
            if (host is IEndpointRouteBuilder)
            {
                throw new InvalidOperationException(SocketWithWebServerMessage);
            }

            return host.Services.GetService<TApp>() ?? throw new InvalidOperationException("Application not registered");
        }

        if (host is not IEndpointRouteBuilder endpoints)
        {
            throw new InvalidOperationException(
                "Receiving activities over HTTP requires a web server. Build the bot with WebApplication.CreateBuilder(), "
                + "or enable Socket Mode with UseSocketMode() in AddTeamsBotApplication.");
        }

        return UseHttp<TApp>(endpoints, routePath);
    }

    private static TApp UseHttp<TApp>(IEndpointRouteBuilder endpoints, string routePath)
        where TApp : TeamsBotApplication
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        if (IsSocketModeEnabled(endpoints.ServiceProvider))
        {
            throw new InvalidOperationException(SocketWithWebServerMessage);
        }

        return endpoints.UseBotApplication<TApp>(routePath);
    }

    private static bool IsSocketModeEnabled(IServiceProvider services)
        => services.GetService<TeamsBotApplicationOptions>()?.SocketMode is not null;

    internal const string SocketWithWebServerMessage =
        "Socket Mode is enabled with UseSocketMode, and it runs without a web server. Build the bot with "
        + "Host.CreateApplicationBuilder() instead of WebApplication.CreateBuilder(), then call host.UseTeamsBotApplication().";

    private static void AddTeamsBotApplicationState(IServiceCollection services, Action<TurnStateOptions>? configure)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<TurnStateOptions>();
        }

        // Provide an in-memory cache as fallback so UseState() works out of the box.
        // AddDistributedMemoryCache() uses TryAdd, so it will not replace an existing IDistributedCache registration.
        // If the developer registers another IDistributedCache later using Add*,
        // the last registration will be resolved (e.g., AddStackExchangeRedisCache).
        services.AddDistributedMemoryCache();

        services.AddSingleton<TurnStateLoader>();
    }
}
