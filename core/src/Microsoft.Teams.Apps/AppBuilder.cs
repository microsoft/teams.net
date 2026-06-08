// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Provides a static entry point for creating an <see cref="AppBuilder"/>.
/// </summary>
public static class App
{
    /// <summary>
    /// Creates a new <see cref="AppBuilder"/> instance for configuring a Teams bot application.
    /// </summary>
    /// <returns>A new <see cref="AppBuilder"/>.</returns>
    public static AppBuilder Builder() => new();
}

/// <summary>
/// Fluent builder for configuring a Teams bot application.
/// Wraps <see cref="TeamsBotApplicationOptions"/> for backward compatibility with the old <c>App.Builder()</c> pattern.
/// </summary>
public class AppBuilder
{
    internal TeamsBotApplicationOptions Options { get; } = new();

    /// <summary>
    /// Registers an OAuth connection for the bot application.
    /// </summary>
    /// <param name="connectionName">The OAuth connection name configured on the bot.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AppBuilder AddOAuth(string connectionName)
    {
        Options.AddOAuthFlow(connectionName);
        return this;
    }

    /// <summary>
    /// Registers turn state backed by the application's <see cref="IDistributedCache"/> (resolved from
    /// DI). If no <see cref="IDistributedCache"/> is registered, <c>AddTeamsBotApplication</c> defaults
    /// to <c>AddDistributedMemoryCache</c>; register a distributed backend (e.g.
    /// <c>AddStackExchangeRedisCache</c>) to override for multi-instance deployments. State loads at the
    /// start of each turn and saves changed scopes when the handler completes successfully.
    /// </summary>
    /// <param name="entryOptions">Optional per-entry options (e.g. expiration) applied to every write.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AppBuilder UseState(DistributedCacheEntryOptions? entryOptions = null)
    {
        Options.UseState(entryOptions);
        return this;
    }
}
