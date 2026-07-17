// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps;

/// <summary>
/// Provides a static entry point for creating an <see cref="AppBuilder"/>.
/// </summary>
[Obsolete("App.Builder() is a backward-compatibility shim for the old library's App.Builder() pattern and will be removed. Configure OAuth flows via DI instead: builder.Services.AddTeamsBotApplication(options => options.AddOAuthFlow(\"connectionName\")).")]
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
[Obsolete("AppBuilder is a backward-compatibility shim for the old library's App.Builder() pattern and will be removed. Configure OAuth flows via DI instead: builder.Services.AddTeamsBotApplication(options => options.AddOAuthFlow(\"connectionName\")).")]
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
}
