// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http.Headers;

using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
/// <param name="managedIdentityOptions">Optional managed identity options for user-assigned managed identity authentication.</param>
internal sealed class BotAuthenticationHandler(
    IAuthorizationHeaderProvider authorizationHeaderProvider,
    ILogger<BotAuthenticationHandler> logger,
    string scope,
    IOptions<ManagedIdentityOptions>? managedIdentityOptions = null) : DelegatingHandler
{
    private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider = authorizationHeaderProvider ?? throw new ArgumentNullException(nameof(authorizationHeaderProvider));
    private readonly ILogger<BotAuthenticationHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly string _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    private readonly IOptions<ManagedIdentityOptions>? _managedIdentityOptions = managedIdentityOptions;
    private static readonly Action<ILogger, string, Exception?> _logAgenticToken =
        LoggerMessage.Define<string>(LogLevel.Debug, new(2), "Acquiring agentic token for app {AgenticAppId}");
    private static readonly Action<ILogger, string, Exception?> _logAppOnlyToken =
        LoggerMessage.Define<string>(LogLevel.Debug, new(3), "Acquiring app-only token for scope: {Scope}");

    /// <summary>
    /// Key used to store the agentic identity in HttpRequestMessage options.
    /// </summary>
    public static readonly HttpRequestOptionsKey<AgenticIdentity?> AgenticIdentityKey = new("AgenticIdentity");

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Options.TryGetValue(AgenticIdentityKey, out AgenticIdentity? agenticIdentity);

        string token = await GetAuthorizationHeaderAsync(agenticIdentity, cancellationToken).ConfigureAwait(false);

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
                //AuthenticationOptionsName = _aadConfigSectionName,
            }
        };

        // Conditionally apply ManagedIdentity configuration if registered
        if (_managedIdentityOptions is not null)
        {
            ManagedIdentityOptions miOptions = _managedIdentityOptions.Value;

            if (!string.IsNullOrEmpty(miOptions.UserAssignedClientId))
            {
                options.AcquireTokenOptions.ManagedIdentity = miOptions;
            }
        }

        if (agenticIdentity is not null &&
            !string.IsNullOrEmpty(agenticIdentity.AgenticAppId) &&
            !string.IsNullOrEmpty(agenticIdentity.AgenticUserId))
        {
            _logAgenticToken(_logger, agenticIdentity.AgenticAppId, null);

            options.WithAgentUserIdentity(agenticIdentity.AgenticAppId, Guid.Parse(agenticIdentity.AgenticUserId));
            string token = await _authorizationHeaderProvider.CreateAuthorizationHeaderAsync([_scope], options, null, cancellationToken).ConfigureAwait(false);
            return token;
        }

        _logAppOnlyToken(_logger, _scope, null);
        string appToken = await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(_scope, options, cancellationToken).ConfigureAwait(false);
        return appToken;
    }
}
