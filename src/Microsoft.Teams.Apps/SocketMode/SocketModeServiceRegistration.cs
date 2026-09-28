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

    /// <summary>
    /// Validates the options and cloud, then registers Socket Mode for <typeparamref name="TApp"/>.
    /// </summary>
    /// <typeparam name="TApp">The application that processes inbound activities.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="botConfig">The resolved bot configuration.</param>
    /// <param name="options">The Socket Mode options.</param>
    /// <exception cref="InvalidOperationException">Thrown when the options are invalid or the cloud is unsupported.</exception>
    internal static void AddSocketMode<TApp>(IServiceCollection services, BotConfig botConfig, SocketModeOptions options)
        where TApp : TeamsBotApplication
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(botConfig);
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();
        EnsureSupportedCloud(botConfig);

        SocketModeTransportOptions transportOptions = options.ToTransportOptions();
        TimeSpan readinessTimeout = options.ReadinessTimeout;
        TimeSpan keepAliveInterval = options.KeepAliveInterval;
        TimeSpan serverTimeout = options.ServerTimeout;
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
                cancellationToken => tokenProvider.GetAppTokenAsync(SocketModeProtocol.BotFrameworkScope, tenantId: null, cancellationToken));
        });

        services.TryAddSingleton<ISocketConnectionFactory>(sp => new SignalRSocketConnectionFactory(
            sp.GetRequiredService<ISocketModeNegotiator>(),
            readinessTimeout,
            keepAliveInterval,
            serverTimeout,
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<SignalRSocketConnectionFactory>()));

        services.TryAddSingleton(sp =>
        {
            TApp app = sp.GetRequiredService<TApp>();
            return new SocketModeTransport(
                transportOptions,
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
        if (!string.Equals(issuer, SocketModeProtocol.PublicCloudTokenIssuer, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Socket Mode is not supported in this cloud environment (tokenIssuer={botConfig.BotTokenIssuer}). "
                + "Socket Mode currently supports only regular production clouds. Use the HTTP inbound transport instead.");
        }
    }
}
