// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.Hosting;

/// <summary>
/// Acquires access tokens for the bot's own identity, or for an Agentic User the bot is acting as, backed by the same Microsoft.Identity.Web provider and named MSAL options the outbound HTTP pipeline uses.
/// <para>The same acquisition <see cref="BotAuthenticationHandler"/> performs on every outbound Bot Framework call, reachable by a caller that is not an HTTP pipeline. The scope is a parameter rather than a constant: the underlying Microsoft.Identity.Web call is resource-agnostic, and the only thing that varies per resource is which scope is asked for.</para>
/// <para><b>A concrete class rather than an interface, deliberately.</b> This SDK's public surface is concrete types; the only two public interfaces it has are extension points a consumer implements, and this is not one. Nothing outside the SDK supplies token acquisition, and anyone who wants to control it can inject the already-public <see cref="IAuthorizationHeaderProvider"/> this is built from. Shipping a class rather than an interface also leaves the shape free to change without breaking an implementor.</para>
/// </summary>
/// <param name="authorizationHeaderProvider">The authorization header provider for acquiring tokens.</param>
/// <param name="authenticationOptionsName">The name of the MSAL configuration options to use for token acquisition. Defaults to "AzureAd".</param>
/// <param name="managedIdentityOptions">Optional managed identity options monitor, applied on the same terms as the outbound pipeline applies it.</param>
public sealed class BotTokenProvider(
    IAuthorizationHeaderProvider authorizationHeaderProvider,
    string? authenticationOptionsName = null,
    IOptionsMonitor<ManagedIdentityOptions>? managedIdentityOptions = null)
{
    private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider = authorizationHeaderProvider ?? throw new ArgumentNullException(nameof(authorizationHeaderProvider));

    /// <summary>Acquire an app-only token for the given scope.</summary>
    /// <param name="scope">The resource scope to request, e.g. <c>https://graph.microsoft.com/.default</c>.</param>
    /// <param name="tenantId">Tenant to acquire against, overriding the one the named MSAL options configure. Pass the tenant the work is being done in, which for a multi-tenant app is not the app's own. <c>null</c> leaves the configured authority alone, which is what a single-tenant app wants.</param>
    /// <param name="cancellationToken">A token to cancel the acquisition.</param>
    /// <returns>The bearer token without its scheme prefix.</returns>
    public async Task<string?> GetAppTokenAsync(string scope, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        AuthorizationHeaderProviderOptions options = CreateOptions(OptionsName, managedIdentityOptions, out _);

        // Left unset rather than defaulted, so a single-tenant app keeps the authority its configuration already
        // resolves. Setting one here would override that, which is only right when the caller knows better.
        if (!string.IsNullOrEmpty(tenantId))
        {
            options.AcquireTokenOptions.Tenant = tenantId;
        }

        return StripScheme(await _authorizationHeaderProvider
            .CreateAuthorizationHeaderForAppAsync(scope, options, cancellationToken)
            .ConfigureAwait(false));
    }

    /// <summary>
    /// Acquire a user-delegated token for an Agentic User, through the federated identity exchange.
    /// <para>Reads as the agent rather than as the app, so it sees what was shared with the agent rather than everything the app may read.</para>
    /// </summary>
    /// <param name="identity">The agentic identity to act as. Returns <c>null</c> when it does not name both an agentic app and an agentic user, which is the shape a blueprint-level identity has.</param>
    /// <param name="scope">The resource scope to request, e.g. <c>https://graph.microsoft.com/.default</c>.</param>
    /// <param name="cancellationToken">A token to cancel the acquisition.</param>
    /// <returns>The bearer token without its scheme prefix, or <c>null</c> when the identity cannot back one.</returns>
    public async Task<string?> GetAgenticUserTokenAsync(AgenticIdentity identity, string scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (string.IsNullOrEmpty(identity.AgenticAppId)
            || string.IsNullOrEmpty(identity.AgenticUserId)
            || !Guid.TryParse(identity.AgenticUserId, out Guid agenticUserGuid))
        {
            return null;
        }

        // No tenant override, matching the outbound pipeline's agentic acquisition: the agent user identity already
        // names the directory the token is minted in.
        AuthorizationHeaderProviderOptions options = CreateOptions(OptionsName, managedIdentityOptions, out _);
        options.WithAgentUserIdentity(identity.AgenticAppId, agenticUserGuid);

        return StripScheme(await _authorizationHeaderProvider
            .CreateAuthorizationHeaderAsync([scope], options, null, cancellationToken)
            .ConfigureAwait(false));
    }

    private string OptionsName => authenticationOptionsName ?? BotConfig.DefaultSectionName;

    /// <summary>
    /// Build the acquisition options for a named MSAL configuration, applying user-assigned managed identity when the named entry configures one.
    /// <para>Shared with <see cref="BotAuthenticationHandler"/> so the two acquisition sites cannot drift on which identity they acquire as.</para>
    /// </summary>
    /// <param name="optionsName">The named MSAL configuration to acquire against.</param>
    /// <param name="managedIdentityOptions">Optional managed identity options monitor.</param>
    /// <param name="appliedManagedIdentity">The managed identity that was applied, or <c>null</c> when none was.</param>
    internal static AuthorizationHeaderProviderOptions CreateOptions(
        string optionsName,
        IOptionsMonitor<ManagedIdentityOptions>? managedIdentityOptions,
        out ManagedIdentityOptions? appliedManagedIdentity)
    {
        AuthorizationHeaderProviderOptions options = new()
        {
            AcquireTokenOptions = new AcquireTokenOptions()
            {
                AuthenticationOptionsName = optionsName,
            }
        };

        appliedManagedIdentity = null;

        // Conditionally apply ManagedIdentity configuration if registered
        if (managedIdentityOptions is not null)
        {
            ManagedIdentityOptions miOptions = managedIdentityOptions.Get(optionsName);

            if (!string.IsNullOrEmpty(miOptions.UserAssignedClientId))
            {
                options.AcquireTokenOptions.ManagedIdentity = miOptions;
                appliedManagedIdentity = miOptions;
            }
        }

        return options;
    }

    private static string StripScheme(string header)
        => header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header["Bearer ".Length..] : header;
}
