// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
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
        private readonly bool _hasBotIdentity;
        private readonly bool _hasAgentIdentity;
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        private readonly ILogger<RoutedTokenAcquisitionService> _logger;

        public RoutedTokenAcquisitionService(
            bool hasBotIdentity,
            bool hasAgentIdentity,
            IAuthorizationHeaderProvider authorizationHeaderProvider,
            ILogger<RoutedTokenAcquisitionService> logger)
        {
            _hasBotIdentity = hasBotIdentity;
            _hasAgentIdentity = hasAgentIdentity;
            _authorizationHeaderProvider = authorizationHeaderProvider;
            _logger = logger;
        }

        public async Task<string> AcquireTokenForBotAsync(string scope, CancellationToken cancellationToken = default)
        {
            if (!_hasBotIdentity)
            {
                throw new InvalidOperationException(
                    "Bot identity (MsalBot) is not configured. Cannot acquire token using bot credentials. " +
                    "Either configure MsalBot section in configuration or use AcquireTokenForAgenticAsync instead.");
            }

            _logger.LogDebug("Acquiring token for bot credentials using MsalBot configuration");

            // Use the bot client credentials configuration
            return await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                scope,
                new AuthorizationHeaderProviderOptions
                {
                    AcquireTokenOptions = new AcquireTokenOptions
                    {
                        AuthenticationOptionsName = "MsalBot"
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

            if (!_hasAgentIdentity)
            {
                throw new InvalidOperationException(
                    "Agent identity (MsalAgent) is not configured. Cannot acquire token using agent credentials. " +
                    "Configure MsalAgent section in configuration to use agentic authentication.");
            }

            _logger.LogDebug("Acquiring token for agentic credentials with AppId '{AppId}' and UserId '{UserId}'",
                agenticIdentity.AgenticAppId,
                agenticIdentity.AgenticUserId);

            // Use the agentic client credentials configuration
            AuthorizationHeaderProviderOptions options = new()
            {
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    AuthenticationOptionsName = "MsalAgent"
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
