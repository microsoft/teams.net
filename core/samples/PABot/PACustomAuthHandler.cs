// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

namespace PABot
{
    internal class PACustomAuthHandler(
        IAuthorizationHeaderProvider authorizationHeaderProvider,
        IRoutedTokenAcquisitionService routedTokenService,
        ILogger<PACustomAuthHandler> logger,
        string botScope,
        string? agenticUserScope = null,
        IOptions<ManagedIdentityOptions>? managedIdentityOptions = null) : DelegatingHandler
    {
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider = authorizationHeaderProvider ?? throw new ArgumentNullException(nameof(authorizationHeaderProvider));
        private readonly IRoutedTokenAcquisitionService _routedTokenService = routedTokenService ?? throw new ArgumentNullException(nameof(routedTokenService));
        private readonly ILogger<PACustomAuthHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly string _botScope = botScope ?? throw new ArgumentNullException(nameof(botScope));
        private readonly string _agenticUserScope = agenticUserScope ?? botScope; // Default to bot scope if not specified
        private readonly IOptions<ManagedIdentityOptions>? _managedIdentityOptions = managedIdentityOptions;

        /// <summary>
        /// Key used to store the agentic user in HttpRequestMessage options.
        /// When set, agentic app instance credentials will be used instead of bot credentials.
        /// </summary>
        public static readonly HttpRequestOptionsKey<AgenticUser?> AgenticUserKey = new(BotRequestContext.AgenticUserKey);

        /// <summary>
        /// Key used to read the bot app id from HttpRequestMessage options.
        /// When set, a token is minted as that specific bot.
        /// </summary>
        public static readonly HttpRequestOptionsKey<string?> BotAppIdKey = new(BotRequestContext.BotAppIdKey);

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Options.TryGetValue(AgenticUserKey, out AgenticUser? agenticUser);

            // The per-request properties (bot app id, etc.) were derived from the activity by core and
            // stamped onto request.Options — no ambient state in this handler. Read the bot app id here.
            request.Options.TryGetValue(BotAppIdKey, out string? botAppId);

            string token = await GetAuthorizationHeaderAsync(agenticUser, botAppId, cancellationToken).ConfigureAwait(false);

            string tokenValue = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? token["Bearer ".Length..]
                : token;

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenValue);

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets an authorization header for API calls.
        /// Routes to either bot credentials or agentic app instance credentials based on the presence of AgenticUser.
        /// </summary>
        /// <param name="agenticUser">Optional agentic user. When provided, agentic app instance credentials are used.</param>
        /// <param name="botAppId">Optional bot application (client) id extracted from the incoming activity. When provided, a token is minted as that specific bot.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The authorization header value.</returns>
        private async Task<string> GetAuthorizationHeaderAsync(AgenticUser? agenticUser, string? botAppId, CancellationToken cancellationToken)
        {
            // If agentic user is provided, use agentic app instance credentials with agentic user scope
            if (agenticUser is not null &&
                !string.IsNullOrEmpty(agenticUser.AgenticAppInstanceId) &&
                !string.IsNullOrEmpty(agenticUser.AgenticUserId))
            {
                _logger.LogInformation("Acquiring token using agentic user credentials for scope '{Scope}' with AppId '{AppId}' and UserId '{UserId}'.",
                    _agenticUserScope,
                    agenticUser.AgenticAppInstanceId,
                    agenticUser.AgenticUserId);

                return await _routedTokenService.AcquireTokenForAgenticUserAsync(agenticUser, _agenticUserScope, cancellationToken).ConfigureAwait(false);
            }

            // If a bot app id was sourced from the activity, mint a token as that specific bot.
            if (!string.IsNullOrEmpty(botAppId))
            {
                _logger.LogInformation("Acquiring token as BotAppId '{BotAppId}' for scope: {Scope}", botAppId, _botScope);
                return await _routedTokenService.AcquireTokenForBotAppIdAsync(botAppId, _botScope, cancellationToken).ConfigureAwait(false);
            }

            // Otherwise, use the default bot credentials with bot scope
            _logger.LogInformation("Acquiring token using default bot credentials for scope: {Scope}", _botScope);
            return await _routedTokenService.AcquireTokenForBotAsync(_botScope, cancellationToken).ConfigureAwait(false);
        }
    }
}
