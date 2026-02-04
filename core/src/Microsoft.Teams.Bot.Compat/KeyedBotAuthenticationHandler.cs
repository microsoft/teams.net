// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Compat;

/// <summary>
/// A delegating handler that adds authentication headers to outgoing HTTP requests
/// using named MSAL configuration options.
/// </summary>
/// <remarks>
/// <para>
/// This handler acquires OAuth tokens using the Microsoft Identity platform and adds
/// them to outgoing requests. It supports both app-only tokens and agentic (user-delegated)
/// tokens when an <see cref="AgenticIdentity"/> is present in the request options.
/// </para>
/// <para>
/// The handler uses named <see cref="MicrosoftIdentityApplicationOptions"/> to support
/// multi-instance scenarios where different bot configurations require different credentials.
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Clarity over performance for this internal class")]
internal sealed class KeyedBotAuthenticationHandler : DelegatingHandler
{
    private readonly string _msalOptionsName;
    private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
    private readonly ILogger<KeyedBotAuthenticationHandler> _logger;
    private readonly string _scope;
    private readonly IOptions<ManagedIdentityOptions>? _managedIdentityOptions;

    /// <summary>
    /// Key used to store the agentic identity in HttpRequestMessage options.
    /// </summary>
    public static readonly HttpRequestOptionsKey<AgenticIdentity?> AgenticIdentityKey = new("AgenticIdentity");

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedBotAuthenticationHandler"/> class.
    /// </summary>
    /// <param name="msalOptionsName">The name of the MSAL configuration options to use for token acquisition.</param>
    /// <param name="authorizationHeaderProvider">The provider used to create authorization headers.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="scope">The OAuth scope for token acquisition.</param>
    /// <param name="managedIdentityOptions">Optional managed identity configuration.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="authorizationHeaderProvider"/>, <paramref name="logger"/>,
    /// or <paramref name="scope"/> is null.
    /// </exception>
    public KeyedBotAuthenticationHandler(
        string msalOptionsName,
        IAuthorizationHeaderProvider authorizationHeaderProvider,
        ILogger<KeyedBotAuthenticationHandler> logger,
        string scope,
        IOptions<ManagedIdentityOptions>? managedIdentityOptions = null)
    {
        _msalOptionsName = msalOptionsName ?? throw new ArgumentNullException(nameof(msalOptionsName));
        _authorizationHeaderProvider = authorizationHeaderProvider ?? throw new ArgumentNullException(nameof(authorizationHeaderProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _managedIdentityOptions = managedIdentityOptions;
    }

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
    private async Task<string> GetAuthorizationHeaderAsync(AgenticIdentity? agenticIdentity, CancellationToken cancellationToken)
    {
        AuthorizationHeaderProviderOptions options = new()
        {
            AcquireTokenOptions = new AcquireTokenOptions()
            {
                AuthenticationOptionsName = _msalOptionsName
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
            _logger.LogInformation(
                "Acquiring agentic token for scope '{Scope}' with AppId '{AppId}' and UserId '{UserId}'.",
                _scope,
                agenticIdentity.AgenticAppId,
                agenticIdentity.AgenticUserId);

            options.WithAgentUserIdentity(agenticIdentity.AgenticAppId, Guid.Parse(agenticIdentity.AgenticUserId));
            string token = await _authorizationHeaderProvider
                .CreateAuthorizationHeaderAsync([_scope], options, null, cancellationToken)
                .ConfigureAwait(false);
            return token;
        }

        _logger.LogInformation("Acquiring app-only token for scope: {Scope}", _scope);
        string appToken = await _authorizationHeaderProvider
            .CreateAuthorizationHeaderForAppAsync(_scope, options, cancellationToken)
            .ConfigureAwait(false);
        return appToken;
    }
}
