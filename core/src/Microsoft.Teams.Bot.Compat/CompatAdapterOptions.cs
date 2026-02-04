// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Bot.Compat;

/// <summary>
/// Configuration options for compat adapter registration.
/// </summary>
/// <remarks>
/// Use this class to configure how a compat adapter instance is registered with the
/// dependency injection container. Options include the configuration section name,
/// token scope, and custom authentication handler factory.
/// </remarks>
public class CompatAdapterOptions
{
    /// <summary>
    /// The default scope for Bot Framework API token acquisition.
    /// </summary>
    public const string DefaultScope = "https://api.botframework.com/.default";

    /// <summary>
    /// Gets or sets the configuration section name for Azure AD / MSAL settings.
    /// </summary>
    /// <remarks>
    /// This value is used to bind MicrosoftIdentityApplicationOptions
    /// from the application configuration. Defaults to "AzureAd".
    /// </remarks>
    /// <value>The configuration section name. Defaults to "AzureAd".</value>
    public string ConfigurationSectionName { get; set; } = "AzureAd";

    /// <summary>
    /// Gets or sets the scope for token acquisition.
    /// </summary>
    /// <remarks>
    /// If not specified, the scope is read from the configuration section's "Scope" property,
    /// or defaults to <see cref="DefaultScope"/> if not found in configuration.
    /// </remarks>
    /// <value>The token scope, or <c>null</c> to use configuration or default.</value>
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets a factory for creating custom <see cref="DelegatingHandler"/> instances
    /// for HTTP authentication.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this property to provide a custom authentication handler when the default
    /// <see cref="KeyedBotAuthenticationHandler"/> does not meet your requirements.
    /// </para>
    /// <para>
    /// The factory receives the service provider, key name, and scope as parameters
    /// and should return a configured <see cref="DelegatingHandler"/> instance.
    /// </para>
    /// <example>
    /// <code>
    /// options.AuthHandlerFactory = (serviceProvider, keyName, scope) =>
    ///     new MyCustomAuthHandler(keyName, scope);
    /// </code>
    /// </example>
    /// </remarks>
    /// <value>
    /// A factory delegate that creates a <see cref="DelegatingHandler"/>, or <c>null</c>
    /// to use the default <see cref="KeyedBotAuthenticationHandler"/>.
    /// </value>
    public Func<IServiceProvider, string, string, DelegatingHandler>? AuthHandlerFactory { get; set; }
}
