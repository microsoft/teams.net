// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Core.Hosting;

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Registers the Socket Mode transport and the hosted service that runs it.
/// </summary>
internal static class SocketModeServiceRegistration
{
    /// <summary>
    /// The name of the plain HTTP client used for negotiate requests. The negotiator attaches its own bot token, so
    /// this client must not carry the bot authentication handler.
    /// </summary>
    internal const string NegotiatorHttpClientName = "SocketModeNegotiator";

    // Core keeps its equivalents internal, so they are restated here for the only two places Socket Mode needs them.
    private const string BotFrameworkScope = "https://api.botframework.com/.default";
    private const string PublicCloudTokenIssuer = "https://api.botframework.com";

    /// <summary>
    /// Checks the cloud, then registers Socket Mode for <typeparamref name="TApp"/>. The options themselves are
    /// validated when the host starts, where the transport and connection factory are created.
    /// </summary>
    /// <typeparam name="TApp">The application that processes inbound activities.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="botConfig">The resolved bot configuration.</param>
    /// <param name="options">The Socket Mode options.</param>
    /// <exception cref="InvalidOperationException">Thrown when the cloud is unsupported.</exception>
#pragma warning disable ExperimentalTeamsSocketMode // Internal wiring for the experimental options passed to UseSocketMode.
    internal static void AddSocketMode<TApp>(IServiceCollection services, BotConfig botConfig, SocketModeOptions options)
#pragma warning restore ExperimentalTeamsSocketMode
        where TApp : TeamsBotApplication
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(botConfig);
        ArgumentNullException.ThrowIfNull(options);

        EnsureSupportedCloud(botConfig);

        // The bot's HTTP authorization services depend on routing, which only a web app registers. Without it a
        // generic host fails service validation (on by default in Development) even though nothing resolves them.
        services.AddRouting();

        string sectionName = botConfig.SectionName;
        string clientId = botConfig.ClientId;

        // The negotiator is a long-lived singleton holding one HttpClient, so connection pooling rotates connections
        // instead of the factory's handler lifetime.
        services.AddHttpClient(NegotiatorHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(2) })
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

        services.TryAddSingleton<ISocketModeNegotiator>(sp =>
        {
            BotTokenProvider tokenProvider = sp.GetRequiredKeyedService<BotTokenProvider>(sectionName);
            return new SocketModeNegotiator(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(NegotiatorHttpClientName),
                cancellationToken => tokenProvider.GetAppTokenAsync(BotFrameworkScope, tenantId: null, cancellationToken));
        });

        services.TryAddSingleton<ISocketConnectionFactory>(sp => new SignalRSocketConnectionFactory(
            sp.GetRequiredService<ISocketModeNegotiator>(),
            options.ReadinessTimeout,
            options.KeepAliveInterval,
            options.ServerTimeout,
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<SignalRSocketConnectionFactory>()));

        services.TryAddSingleton(sp =>
        {
            TApp app = sp.GetRequiredService<TApp>();
            return new SocketModeTransport(
                options.ToTransportOptions(),
                sp.GetRequiredService<ISocketConnectionFactory>(),
                activity => DispatchAsync(app, activity),
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<SocketModeTransport>(),
                botKey: clientId);
        });

        services.AddHostedService<SocketModeHostedService>();
    }

    /// <summary>
    /// Runs an activity through the app's pipeline and converts its invoke response to a reply status and body.
    /// </summary>
    /// <param name="app">The application that processes the activity.</param>
    /// <param name="activity">The inbound activity.</param>
    /// <returns>The status and body to return over the socket; 200 with no body when there is no invoke response.</returns>
    internal static async Task<SocketDispatchResult> DispatchAsync(TeamsBotApplication app, Core.Schema.CoreActivity activity)
    {
        // No per-activity principal: the connection itself was authenticated with the bot's own token.
        InvokeResponse? response = await app.ProcessWithInvokeResponseAsync(activity, user: null, correlationVector: null).ConfigureAwait(false);
        return response is null
            ? new SocketDispatchResult(200)
            : new SocketDispatchResult(response.Status, response.Body);
    }

    private static void EnsureSupportedCloud(BotConfig botConfig)
    {
        string issuer = botConfig.BotTokenIssuer?.Trim().TrimEnd('/') ?? string.Empty;
        if (!string.Equals(issuer, PublicCloudTokenIssuer, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Socket Mode is not supported in this cloud environment (tokenIssuer={botConfig.BotTokenIssuer}). "
                + "Socket Mode currently supports only regular production clouds. Use the HTTP inbound transport instead.");
        }
    }
}
