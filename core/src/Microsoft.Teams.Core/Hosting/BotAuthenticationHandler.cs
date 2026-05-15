// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.Hosting;

/// <summary>
/// HTTP message handler that automatically acquires and attaches authentication tokens
/// for Bot Framework API calls. Supports both app-only and agentic (user-delegated) token acquisition.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BotAuthenticationHandler"/> class.
/// </remarks>
/// <param name="authorizationHeaderProvider">The authorization header provider for acquiring tokens.</param>
/// <param name="logger">The logger instance.</param>
/// <param name="authenticationOptionsName">The name of the MSAL configuration options to use for token acquisition. Defaults to "AzureAd".</param>
/// <param name="managedIdentityOptions">Optional managed identity options monitor. When the named entry matching <paramref name="authenticationOptionsName"/> has a non-empty <c>UserAssignedClientId</c>, tokens are acquired via the IMDS endpoint as the configured managed identity instead of via the app-credentials flow.</param>
internal sealed class BotAuthenticationHandler(
    IAuthorizationHeaderProvider authorizationHeaderProvider,
    ILogger<BotAuthenticationHandler> logger,
    string? authenticationOptionsName = null,
    IOptionsMonitor<ManagedIdentityOptions>? managedIdentityOptions = null) : DelegatingHandler
{
    private const string AgenticScope = "https://botapi.skype.com/.default";
    private const string BotAppScope = "https://api.botframework.com/.default";

    private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider = authorizationHeaderProvider ?? throw new ArgumentNullException(nameof(authorizationHeaderProvider));
    private readonly ILogger<BotAuthenticationHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IOptionsMonitor<ManagedIdentityOptions>? _managedIdentityOptions = managedIdentityOptions;
    private static readonly Action<ILogger, string, Exception?> _logAgenticToken =
        LoggerMessage.Define<string>(LogLevel.Debug, new(2), "Acquiring agentic token for AgenticAppId {AgenticAppId}");
    private static readonly Action<ILogger, string, Exception?> _logAppOnlyToken =
        LoggerMessage.Define<string>(LogLevel.Debug, new(3), "Acquiring app-only token for scope: {Scope}");
    private static readonly Action<ILogger, string, Exception?> _logTokenClaims =
        LoggerMessage.Define<string>(LogLevel.Trace, new(4), "Acquired token claims:{Claims}");
    private static readonly Action<ILogger, string, Exception?> _logInvalidAgenticUserId =
        LoggerMessage.Define<string>(LogLevel.Warning, new(5), "Invalid AgenticUserId '{AgenticUserId}'; falling back to app-only token.");
    private static readonly Action<ILogger, Exception?> _logTokenParseFailure =
        LoggerMessage.Define(LogLevel.Warning, new(6), "Failed to parse JWT token for trace logging.");

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

        LogTokenClaims(tokenValue);

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
        string optionsName = authenticationOptionsName ?? BotConfig.DefaultSectionName;
        AuthorizationHeaderProviderOptions options = new()
        {
            AcquireTokenOptions = new AcquireTokenOptions()
            {
                AuthenticationOptionsName = optionsName,
            }
        };

        if (_managedIdentityOptions?.Get(optionsName) is { UserAssignedClientId.Length: > 0 } miOptions)
        {
            options.AcquireTokenOptions.ManagedIdentity = miOptions;
        }

        if (agenticIdentity is not null &&
            !string.IsNullOrEmpty(agenticIdentity.AgenticAppId) &&
            !string.IsNullOrEmpty(agenticIdentity.AgenticUserId))
        {
            _logAgenticToken(_logger, agenticIdentity.AgenticAppId, null);

            if (!Guid.TryParse(agenticIdentity.AgenticUserId, out Guid agenticUserGuid))
            {
                _logInvalidAgenticUserId(_logger, agenticIdentity.AgenticUserId, null);
            }
            else
            {
                options.WithAgentUserIdentity(agenticIdentity.AgenticAppId, agenticUserGuid);
                string token = await _authorizationHeaderProvider.CreateAuthorizationHeaderAsync([AgenticScope], options, null, cancellationToken).ConfigureAwait(false);
                return token;
            }
        }

        _logAppOnlyToken(_logger, BotAppScope, null);
        string appToken = await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(BotAppScope, options, cancellationToken).ConfigureAwait(false);


        return appToken;
    }

    private void LogTokenClaims(string token)
    {
        if (!_logger.IsEnabled(LogLevel.Trace))
        {
            return;
        }


        try
        {
            JwtSecurityToken jwtToken = new(token);
            string claims = Environment.NewLine + string.Join(Environment.NewLine, jwtToken.Claims.Select(c => $"  {c.Type}: {c.Value}"));
            _logTokenClaims(_logger, claims, null);
        }
        catch (ArgumentException ex)
        {
            _logTokenParseFailure(_logger, ex);
        }
    }
}
