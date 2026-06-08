// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Teams.Apps.OAuth;
using Microsoft.Teams.Apps.State;
using Microsoft.Teams.Core.Hosting;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Options for configuring a <see cref="TeamsBotApplication"/>.
/// Inherits <see cref="BotApplicationOptions"/> so a single options object covers both Core and Teams settings.
/// </summary>
public sealed class TeamsBotApplicationOptions : BotApplicationOptions
{
    internal List<OAuthFlowDescriptor> OAuthFlows { get; } = [];

    /// <summary>True when <see cref="UseState"/> has been called; a <see cref="TurnStateStore"/> is
    /// registered in DI and resolved by the bot on its first request.</summary>
    internal bool StateEnabled { get; private set; }

    /// <summary>Per-entry options (e.g. expiration) applied to every state document written.</summary>
    internal DistributedCacheEntryOptions? StateEntryOptions { get; private set; }

    /// <summary>
    /// Registers turn state backed by the application's <see cref="IDistributedCache"/> (resolved from
    /// DI). Register a cache first — e.g. <c>AddDistributedMemoryCache</c> for in-process dev, or
    /// <c>AddStackExchangeRedisCache</c> for multi-instance deployments. State loads at the start of
    /// each turn and saves changed scopes when the handler completes successfully.
    /// </summary>
    /// <param name="entryOptions">Optional per-entry options (e.g. expiration) applied to every write.</param>
    /// <returns>This instance for chaining.</returns>
    public TeamsBotApplicationOptions UseState(DistributedCacheEntryOptions? entryOptions = null)
    {
        StateEnabled = true;
        StateEntryOptions = entryOptions;
        return this;
    }

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
        return this;
    }

    internal sealed record OAuthFlowDescriptor(string ConnectionName, OAuthOptions Options);
}
