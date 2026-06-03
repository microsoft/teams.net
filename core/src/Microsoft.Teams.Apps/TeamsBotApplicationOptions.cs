// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.OAuth;
using Microsoft.Teams.Core.Hosting;
using Microsoft.Teams.Core.State;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Options for configuring a <see cref="TeamsBotApplication"/>.
/// Inherits <see cref="BotApplicationOptions"/> so a single options object covers both Core and Teams settings.
/// </summary>
public sealed class TeamsBotApplicationOptions : BotApplicationOptions
{
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
        return this;
    }

    internal bool UseState { get; private set; }

    internal Action<TurnStateOptions>? StateConfiguration { get; private set; }

    /// <summary>
    /// Enables per-turn state management backed by <c>IDistributedCache</c>.
    /// An in-memory cache is used by default; register a custom <c>IDistributedCache</c>
    /// (Redis, SQL Server, etc.) to persist state across restarts.
    /// </summary>
    /// <param name="configure">Optional delegate to configure <see cref="TurnStateOptions"/> (e.g. cache entry TTL).</param>
    /// <returns>This instance for chaining.</returns>
    public TeamsBotApplicationOptions WithState(Action<TurnStateOptions>? configure = null)
    {
        UseState = true;
        StateConfiguration = configure;
        return this;
    }

    internal sealed record OAuthFlowDescriptor(string ConnectionName, OAuthOptions Options);
}
