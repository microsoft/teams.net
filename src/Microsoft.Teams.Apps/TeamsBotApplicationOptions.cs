// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Teams.Apps.OAuth;
using Microsoft.Teams.Apps.SocketMode;
using Microsoft.Teams.Apps.State;
using Microsoft.Teams.Core.Hosting;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Options for configuring a <see cref="TeamsBotApplication"/>.
/// Inherits <see cref="BotApplicationOptions"/> so a single options object covers both Core and Teams settings.
/// </summary>
public sealed class TeamsBotApplicationOptions : BotApplicationOptions
{
    /// <summary>
    /// Microsoft Graph host root the inbound-file path resolves drive items against, for files that arrive without a pre-authorized download URL.
    /// Populated from <c>BotFramework:GraphBaseUrl</c> by the hosting extensions; <c>null</c> uses the public cloud.
    /// <para>A host root, not a versioned endpoint: the API version is appended at the point of use, so a pre-versioned value produces <c>/v1.0/v1.0</c>.</para>
    /// </summary>
    public Uri? GraphBaseUrl { get; set; }

    internal List<OAuthFlowDescriptor> OAuthFlows { get; } = [];

    /// <summary>
    /// Register an OAuth flow with the given connection name and optional configuration.
    /// </summary>
    /// <param name="connectionName">The OAuth connection name configured on the bot.</param>
    /// <param name="configure">Optional delegate to configure the <see cref="OAuthOptions"/> (card text, button text).</param>
    /// <returns>This instance for chaining.</returns>
    public TeamsBotApplicationOptions AddOAuthFlow(string connectionName, Action<OAuthOptions>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionName);

        OAuthOptions options = new() { ConnectionName = connectionName };
        configure?.Invoke(options);

        OAuthFlows.Add(new OAuthFlowDescriptor(connectionName, options));
        IsStateEnabled = true; // OAuthFlows require state; enable without overwriting existing StateConfiguration.
        return this;
    }

    internal bool IsStateEnabled { get; private set; }

    internal Action<TurnStateOptions>? StateConfiguration { get; private set; }

    /// <summary>
    /// Enables per-turn state management backed by <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>.
    /// An in-memory cache is used by default; register a custom <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>
    /// (Redis, SQL Server, etc.) to persist state across restarts.
    /// </summary>
    /// <param name="configure">Optional delegate to configure <see cref="TurnStateOptions"/> (e.g. cache entry TTL).</param>
    /// <returns>This instance for chaining.</returns>
    public TeamsBotApplicationOptions UseState(Action<TurnStateOptions>? configure = null)
    {
        IsStateEnabled = true;
        StateConfiguration = configure;
        return this;
    }

    // The SDK's own Socket Mode wiring uses the experimental options; the diagnostic is for callers of UseSocketMode.
#pragma warning disable ExperimentalTeamsSocketMode
    internal SocketModeOptions? SocketMode { get; private set; }
#pragma warning restore ExperimentalTeamsSocketMode

    /// <summary>
    /// Enables or disables Socket Mode with the default <see cref="SocketModeOptions"/>. Socket Mode receives activities
    /// over outbound WebSocket connections instead of an inbound HTTP endpoint.
    /// </summary>
    /// <remarks>
    /// The host does not finish starting until every configured geo is connected, and a startup failure stops the host.
    /// Supported only in the public cloud. Build the bot with <c>Host.CreateApplicationBuilder()</c> (no web server) and
    /// get the app with <c>host.UseTeamsBotApplication()</c>. Passing <see langword="false"/> clears any earlier Socket
    /// Mode configuration, so the bot receives activities over HTTP. Socket Mode is experimental: the API is in preview
    /// and may change, and it is recommended only for developing agents.
    /// </remarks>
    /// <param name="enabled">Whether to receive activities over Socket Mode. Default is <see langword="true"/>.</param>
    /// <returns>This instance for chaining.</returns>
    [Experimental("ExperimentalTeamsSocketMode")]
    public TeamsBotApplicationOptions UseSocketMode(bool enabled = true)
    {
        SocketMode = enabled ? new SocketModeOptions() : null;
        return this;
    }

    /// <summary>
    /// Enables Socket Mode and configures its <see cref="SocketModeOptions"/>, such as the negotiate URL, geos, and
    /// connection timeouts. Socket Mode receives activities over outbound WebSocket connections instead of an inbound
    /// HTTP endpoint.
    /// </summary>
    /// <remarks>
    /// The host does not finish starting until every configured geo is connected, and a startup failure stops the host.
    /// Supported only in the public cloud. Build the bot with <c>Host.CreateApplicationBuilder()</c> (no web server) and
    /// get the app with <c>host.UseTeamsBotApplication()</c>. Socket Mode is experimental: the API is in preview and may
    /// change, and it is recommended only for developing agents.
    /// </remarks>
    /// <param name="configure">Delegate to configure <see cref="SocketModeOptions"/>.</param>
    /// <returns>This instance for chaining.</returns>
    [Experimental("ExperimentalTeamsSocketMode")]
    public TeamsBotApplicationOptions UseSocketMode(Action<SocketModeOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        SocketModeOptions options = new();
        configure(options);
        SocketMode = options;
        return this;
    }

    internal sealed record OAuthFlowDescriptor(string ConnectionName, OAuthOptions Options);
}
