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
        IAuthorizationHeaderProvider authorizationHeaderProvider,
        IRoutedTokenAcquisitionService routedTokenService,
        ILogger<PACustomAuthHandler> logger,
        string botScope,
        string? agenticScope = null,
        IOptions<ManagedIdentityOptions>? managedIdentityOptions = null) : DelegatingHandler
    {
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider = authorizationHeaderProvider ?? throw new ArgumentNullException(nameof(authorizationHeaderProvider));
        private readonly IRoutedTokenAcquisitionService _routedTokenService = routedTokenService ?? throw new ArgumentNullException(nameof(routedTokenService));
        private readonly ILogger<PACustomAuthHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly string _botScope = botScope ?? throw new ArgumentNullException(nameof(botScope));
        private readonly string _agenticScope = agenticScope ?? botScope; // Default to bot scope if not specified
        private readonly IOptions<ManagedIdentityOptions>? _managedIdentityOptions = managedIdentityOptions;

        /// <summary>
        /// Key used to store the agentic identity in HttpRequestMessage options.
        /// When set, agentic application credentials will be used instead of bot credentials.
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
        /// Gets an authorization header for API calls.
        /// Routes to either bot credentials or agentic application credentials based on the presence of AgenticIdentity.
        /// </summary>
        /// <param name="agenticIdentity">Optional agentic identity. When provided, agentic application credentials are used.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The authorization header value.</returns>
        private async Task<string> GetAuthorizationHeaderAsync(AgenticIdentity? agenticIdentity, CancellationToken cancellationToken)
        {
            // If agentic identity is provided, use agentic application credentials with agentic scope
            if (agenticIdentity is not null &&
                !string.IsNullOrEmpty(agenticIdentity.AgenticAppId) &&
                !string.IsNullOrEmpty(agenticIdentity.AgenticUserId))
            {
                _logger.LogInformation("Acquiring token using agentic credentials for scope '{Scope}' with AppId '{AppId}' and UserId '{UserId}'.",
                    _agenticScope,
                    agenticIdentity.AgenticAppId,
                    agenticIdentity.AgenticUserId);

                return await _routedTokenService.AcquireTokenForAgenticAsync(agenticIdentity, _agenticScope, cancellationToken).ConfigureAwait(false);
            }

            // Otherwise, use bot credentials with bot scope
            _logger.LogInformation("Acquiring token using bot credentials for scope: {Scope}", _botScope);
            return await _routedTokenService.AcquireTokenForBotAsync(_botScope, cancellationToken).ConfigureAwait(false);
        }
    }
}
