// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

    /// <summary>
    /// The storage backing turn state, set by <see cref="UseState"/>. When non-null, a
    /// <see cref="StateMiddleware"/> is registered as the bot is constructed.
    /// </summary>
    internal IStorage? StateStorage { get; private set; }

    /// <summary>
    /// Registers turn state backed by the given storage. State loads at the start of each turn and
    /// saves changed scopes when the handler completes successfully.
    /// </summary>
    /// <param name="storage">The backing store for state documents.</param>
    /// <returns>This instance for chaining.</returns>
    public TeamsBotApplicationOptions UseState(IStorage storage)
    {
        ArgumentNullException.ThrowIfNull(storage);
        StateStorage = storage;
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
