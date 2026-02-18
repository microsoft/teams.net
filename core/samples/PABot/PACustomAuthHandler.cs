// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Teams.Bot.Core.Schema;

namespace PABot
{
    internal class PACustomAuthHandler(
        string msalOptionName,
        IAuthorizationHeaderProvider authorizationHeaderProvider,
        ILogger<PACustomAuthHandler> logger,
        string scope,
        IOptions<ManagedIdentityOptions>? managedIdentityOptions = null) : DelegatingHandler
    {
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider = authorizationHeaderProvider ?? throw new ArgumentNullException(nameof(authorizationHeaderProvider));
        private readonly ILogger<PACustomAuthHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly string _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        private readonly IOptions<ManagedIdentityOptions>? _managedIdentityOptions = managedIdentityOptions;
        
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
                    AuthenticationOptionsName = msalOptionName
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
                _logger.LogInformation("Acquiring agentic token for scope '{Scope}' with AppId '{AppId}' and UserId '{UserId}'.",
                    _scope,
                    agenticIdentity.AgenticAppId,
                    agenticIdentity.AgenticUserId);

                options.WithAgentUserIdentity(agenticIdentity.AgenticAppId, Guid.Parse(agenticIdentity.AgenticUserId));
                string token = await _authorizationHeaderProvider.CreateAuthorizationHeaderAsync([_scope], options, null, cancellationToken).ConfigureAwait(false);
                return token;
            }

            _logger.LogInformation("Acquiring app-only token for scope: {Scope}", _scope);
            string appToken = await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(_scope, options, cancellationToken).ConfigureAwait(false);
            return appToken;
        }
    }
}
