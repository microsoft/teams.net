// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Options;
using Microsoft.Teams.Bot.Core.Schema;

namespace PABot
{
    /// <summary>
    /// Token acquisition service that routes to either bot or agentic credentials based on context.
    /// </summary>
    public interface IRoutedTokenAcquisitionService
    {
        /// <summary>
        /// Acquires a token using bot (channel) credentials.
        /// </summary>
        /// <param name="scope">The scope for the token request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An access token.</returns>
        Task<string> AcquireTokenForBotAsync(string scope, CancellationToken cancellationToken = default);

        /// <summary>
        /// Acquires a token using agentic application credentials.
        /// </summary>
        /// <param name="agenticIdentity">The agentic identity containing AgenticAppId and AgenticUserId.</param>
        /// <param name="scope">The scope for the token request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An access token.</returns>
        Task<string> AcquireTokenForAgenticAsync(AgenticIdentity agenticIdentity, string scope, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Implementation of routed token acquisition service for a specific keyed adapter.
    /// </summary>
    public class RoutedTokenAcquisitionService : IRoutedTokenAcquisitionService
    {
        private readonly string _keyName;
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        private readonly ILogger<RoutedTokenAcquisitionService> _logger;

        public RoutedTokenAcquisitionService(
            string keyName,
            IAuthorizationHeaderProvider authorizationHeaderProvider,
            ILogger<RoutedTokenAcquisitionService> logger)
        {
            _keyName = keyName;
            _authorizationHeaderProvider = authorizationHeaderProvider;
            _logger = logger;
        }

        public async Task<string> AcquireTokenForBotAsync(string scope, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Acquiring token for bot credentials using key: {KeyName}", _keyName);

            // Use the bot client credentials configuration
            return await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                scope,
                new AuthorizationHeaderProviderOptions
                {
                    AcquireTokenOptions = new AcquireTokenOptions
                    {
                        AuthenticationOptionsName = _keyName
                    }
                },
                cancellationToken);
        }

        public async Task<string> AcquireTokenForAgenticAsync(AgenticIdentity agenticIdentity, string scope, CancellationToken cancellationToken = default)
        {
            if (agenticIdentity is null)
            {
                throw new ArgumentNullException(nameof(agenticIdentity));
            }

            if (string.IsNullOrEmpty(agenticIdentity.AgenticAppId))
            {
                throw new ArgumentException("AgenticAppId cannot be null or empty", nameof(agenticIdentity));
            }

            if (string.IsNullOrEmpty(agenticIdentity.AgenticUserId))
            {
                throw new ArgumentException("AgenticUserId cannot be null or empty", nameof(agenticIdentity));
            }

            _logger.LogDebug("Acquiring token for agentic credentials with AppId '{AppId}' and UserId '{UserId}'",
                agenticIdentity.AgenticAppId,
                agenticIdentity.AgenticUserId);

            // Use the agentic client credentials configuration
            string agenticKeyName = $"{_keyName}_Agentic";

            AuthorizationHeaderProviderOptions options = new()
            {
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    AuthenticationOptionsName = agenticKeyName
                }
            };

            // Use WithAgentUserIdentity to acquire token with agentic identity
            options.WithAgentUserIdentity(agenticIdentity.AgenticAppId, Guid.Parse(agenticIdentity.AgenticUserId));

            return await _authorizationHeaderProvider.CreateAuthorizationHeaderAsync(
                [scope],
                options,
                null,
                cancellationToken);
        }
    }
}
