// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.State;

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
    /// Registers turn state backed by the given storage. State loads at the start of each turn and
    /// saves changed scopes when the handler completes successfully.
    /// </summary>
    /// <param name="storage">The backing store for state documents.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AppBuilder UseState(IStorage storage)
    {
        Options.UseState(storage);
        return this;
    }
}
