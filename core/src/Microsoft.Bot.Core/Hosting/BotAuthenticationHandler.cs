// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http.Headers;

using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace Microsoft.Bot.Core.Hosting;


/// <summary>
/// Represents an agentic identity for user-delegated token acquisition.
/// </summary>
internal sealed class AgenticIdentity
{
    public string? AgenticAppId { get; set; }
    public string? AgenticUserId { get; set; }
    public string? AgenticAppBlueprintId { get; set; }

    public static AgenticIdentity? FromProperties(ExtendedPropertiesDictionary? properties)
    {
        if (properties is null)
        {
            return null;
        }

        properties.TryGetValue("agenticAppId", out object? appIdObj);
        properties.TryGetValue("agenticUserId", out object? userIdObj);
        properties.TryGetValue("agenticAppBlueprintId", out object? bluePrintObj);
        return new AgenticIdentity
        {
            AgenticAppId = appIdObj?.ToString(),
            AgenticUserId = userIdObj?.ToString(),
            AgenticAppBlueprintId = bluePrintObj?.ToString()
        };
    }
}

/// <summary>
/// HTTP message handler that automatically acquires and attaches authentication tokens
/// for Bot Framework API calls. Supports both app-only and agentic (user-delegated) token acquisition.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BotAuthenticationHandler"/> class.
/// </remarks>
/// <param name="authorizationHeaderProvider">The authorization header provider for acquiring tokens.</param>
/// <param name="logger">The logger instance.</param>
/// <param name="scope">The scope for the token request.</param>
/// <param name="aadConfigSectionName">The configuration section name for Azure AD settings.</param>
internal sealed class BotAuthenticationHandler(
    IAuthorizationHeaderProvider authorizationHeaderProvider,
    ILogger<BotAuthenticationHandler> logger,
    string scope,
    string aadConfigSectionName = "AzureAd") : DelegatingHandler
{
    private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider = authorizationHeaderProvider ?? throw new ArgumentNullException(nameof(authorizationHeaderProvider));
    private readonly ILogger<BotAuthenticationHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly string _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    private readonly string _aadConfigSectionName = aadConfigSectionName ?? throw new ArgumentNullException(nameof(aadConfigSectionName));

    private static readonly Action<ILogger, string, string, Exception?> LogAcquiringAgenticToken =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(1, nameof(LogAcquiringAgenticToken)),
            "Acquiring agentic token for appId: {AgenticAppId}, userId: {AgenticUserId}");

    private static readonly Action<ILogger, string, Exception?> LogAcquiringAppOnlyToken =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(2, nameof(LogAcquiringAppOnlyToken)),
            "Acquiring app-only token for scope: {Scope}");

    /// </summary>
    /// Key used to store the agentic identity in HttpRequestMessage options.
    /// </summary>
    public static readonly HttpRequestOptionsKey<AgenticIdentity?> AgenticIdentityKey = new("AgenticIdentity");

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Options.TryGetValue(AgenticIdentityKey, out AgenticIdentity? agenticIdentity);

        // TEMPORARY: Hardcoded Managed Identity test
        const string HARDCODED_MANAGED_IDENTITY_CLIENT_ID = "36cc4d80-a643-49fc-8956-47afc1521748";
        string token = await GetTokenUsingManagedIdentityAsync(HARDCODED_MANAGED_IDENTITY_CLIENT_ID, cancellationToken).ConfigureAwait(false);
        // string token = await GetAuthorizationHeaderAsync(agenticIdentity, cancellationToken).ConfigureAwait(false);

        string tokenValue = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? token["Bearer ".Length..]
            : token;

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenValue);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an authorization header for Bot Framework API calls.
    /// Supports both app-only and agentic (user-delegated) token acquisition.
    /// </summary>
    /// <param name="agenticIdentity">Optional agentic identity for user-delegated token acquisition. If not provided, acquires an app-only token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authorization header value.</returns>
    private async Task<string> GetAuthorizationHeaderAsync(AgenticIdentity? agenticIdentity, CancellationToken cancellationToken)
    {
        AuthorizationHeaderProviderOptions options = new()
        {
            AcquireTokenOptions = new AcquireTokenOptions()
            {
                AuthenticationOptionsName = _aadConfigSectionName,
            }
        };

        if (agenticIdentity is not null &&
            !string.IsNullOrEmpty(agenticIdentity.AgenticAppId) &&
            !string.IsNullOrEmpty(agenticIdentity.AgenticUserId))
        {
            LogAcquiringAgenticToken(_logger, agenticIdentity.AgenticAppId, agenticIdentity.AgenticUserId, null);

            options.WithAgentUserIdentity(agenticIdentity.AgenticAppId, Guid.Parse(agenticIdentity.AgenticUserId));
            string token = await _authorizationHeaderProvider.CreateAuthorizationHeaderAsync([_scope], options, null, cancellationToken).ConfigureAwait(false);
            return token;
        }

        LogAcquiringAppOnlyToken(_logger, _scope, null);
        string appToken = await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(_scope, options, cancellationToken).ConfigureAwait(false);
        return appToken;
    }

    /// <summary>
    /// Gets a token using User-Assigned Managed Identity via Microsoft.Identity.Web.
    /// Based on: https://github.com/AzureAD/microsoft-identity-web/wiki/Calling-APIs-with-Managed-Identity
    /// </summary>
    /// <param name="managedIdentityClientId">The Client ID (GUID) of the user-assigned managed identity. Required.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authorization header value.</returns>
    /// <remarks>
    /// This method uses Microsoft.Identity.Web's built-in managed identity support to acquire tokens
    /// without requiring ClientId/ClientSecret/TenantId in the AzureAd configuration section.
    ///
    /// The managed identity must be assigned to the Azure resource (App Service, VM, etc.) and must have
    /// the appropriate permissions to access the Bot Framework API.
    ///
    /// To use this method, set the UseManagedIdentityKey and ManagedIdentityClientIdKey options on the HttpRequestMessage.
    /// </remarks>
    private async Task<string> GetTokenUsingManagedIdentityAsync(string? managedIdentityClientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(managedIdentityClientId))
        {
            throw new ArgumentException("Managed Identity Client ID is required when using UseManagedIdentityKey.", nameof(managedIdentityClientId));
        }

        LogAcquiringManagedIdentityToken(_logger, _scope, managedIdentityClientId, null);

        // Configure options with ManagedIdentity settings
        // This follows the pattern from: https://github.com/AzureAD/microsoft-identity-web/wiki/Calling-APIs-with-Managed-Identity
        AuthorizationHeaderProviderOptions options = new()
        {
            AcquireTokenOptions = new AcquireTokenOptions()
            {
                AuthenticationOptionsName = _aadConfigSectionName,
                ManagedIdentity = new ManagedIdentityOptions
                {
                    UserAssignedClientId = managedIdentityClientId
                }
            }
        };

        // Use CreateAuthorizationHeaderForAppAsync - Microsoft.Identity.Web will detect the ManagedIdentity option
        // and use managed identity instead of client credentials
        string token = await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(_scope, options, cancellationToken).ConfigureAwait(false);
        return token;
    }
}
