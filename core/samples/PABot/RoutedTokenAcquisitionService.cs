// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Teams.Core.Schema;

namespace PABot
{
    /// <summary>
    /// Token acquisition service that routes to either bot or agentic user credentials based on context.
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
        /// Acquires a token minted as the specified bot application (client) id, using that app's
        /// registered credentials. Falls back to the default bot credentials when the app id is not a
        /// configured/trusted bot.
        /// </summary>
        /// <param name="botAppId">The bot application (client) id to authenticate as, sourced from the incoming activity.</param>
        /// <param name="scope">The scope for the token request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An access token.</returns>
        Task<string> AcquireTokenForBotAppIdAsync(string botAppId, string scope, CancellationToken cancellationToken = default);

        /// <summary>
        /// Acquires a token using agentic app instance credentials.
        /// </summary>
        /// <param name="agenticUser">The agentic user containing AgenticAppInstanceId and AgenticUserId.</param>
        /// <param name="scope">The scope for the token request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An access token.</returns>
        Task<string> AcquireTokenForAgenticUserAsync(AgenticUser agenticUser, string scope, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Implementation of routed token acquisition service for a specific keyed adapter.
    /// </summary>
    public class RoutedTokenAcquisitionService : IRoutedTokenAcquisitionService
    {
        private const string BotOptionsName = "MsalBot";

        private readonly bool _hasBotIdentity;
        private readonly bool _hasAgentIdentity;
        private readonly HashSet<string> _trustedBotAppIds;
        private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;
        private readonly ILogger<RoutedTokenAcquisitionService> _logger;

        public RoutedTokenAcquisitionService(
            bool hasBotIdentity,
            bool hasAgentIdentity,
            IEnumerable<string> trustedBotAppIds,
            IAuthorizationHeaderProvider authorizationHeaderProvider,
            ILogger<RoutedTokenAcquisitionService> logger)
        {
            _hasBotIdentity = hasBotIdentity;
            _hasAgentIdentity = hasAgentIdentity;
            _trustedBotAppIds = new HashSet<string>(trustedBotAppIds ?? [], StringComparer.OrdinalIgnoreCase);
            _authorizationHeaderProvider = authorizationHeaderProvider;
            _logger = logger;
        }

        public async Task<string> AcquireTokenForBotAsync(string scope, CancellationToken cancellationToken = default)
        {
            if (!_hasBotIdentity)
            {
                throw new InvalidOperationException(
                    "Bot identity (MsalBot) is not configured. Cannot acquire token using bot credentials. " +
                    "Either configure MsalBot section in configuration or use AcquireTokenForAgenticUserAsync instead.");
            }

            _logger.LogDebug("Acquiring token for bot credentials using MsalBot configuration");

            // Use the bot client credentials configuration
            return await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                scope,
                new AuthorizationHeaderProviderOptions
                {
                    AcquireTokenOptions = new AcquireTokenOptions
                    {
                        AuthenticationOptionsName = BotOptionsName
                    }
                },
                cancellationToken);
        }

        public async Task<string> AcquireTokenForBotAppIdAsync(string botAppId, string scope, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(botAppId);

            // Each configured bot is registered as a named MicrosoftIdentityApplicationOptions keyed by its
            // app (client) id, so selecting that name mints a token AS that specific bot. When the incoming
            // app id is not a configured/trusted bot, fall back to the default bot credentials.
            if (!_trustedBotAppIds.Contains(botAppId))
            {
                _logger.LogWarning("BotAppId '{BotAppId}' is not a configured/trusted bot; falling back to default bot credentials.", botAppId);
                return await AcquireTokenForBotAsync(scope, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogDebug("Acquiring token as bot app id '{BotAppId}' using its registered credentials.", botAppId);

            return await _authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(
                scope,
                new AuthorizationHeaderProviderOptions
                {
                    AcquireTokenOptions = new AcquireTokenOptions
                    {
                        AuthenticationOptionsName = botAppId
                    }
                },
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<string> AcquireTokenForAgenticUserAsync(AgenticUser agenticUser, string scope, CancellationToken cancellationToken = default)
        {
            if (agenticUser is null)
            {
                throw new ArgumentNullException(nameof(agenticUser));
            }

            if (string.IsNullOrEmpty(agenticUser.AgenticAppInstanceId))
            {
                throw new ArgumentException("AgenticAppInstanceId cannot be null or empty", nameof(agenticUser));
            }

            if (string.IsNullOrEmpty(agenticUser.AgenticUserId))
            {
                throw new ArgumentException("AgenticUserId cannot be null or empty", nameof(agenticUser));
            }

            if (!_hasAgentIdentity)
            {
                throw new InvalidOperationException(
                    "Agent identity (MsalAgent) is not configured. Cannot acquire token using agent credentials. " +
                    "Configure MsalAgent section in configuration to use agentic user authentication.");
            }

            _logger.LogDebug("Acquiring token for agentic user credentials with AppId '{AppId}' and UserId '{UserId}'",
                agenticUser.AgenticAppInstanceId,
                agenticUser.AgenticUserId);

            // Use the agentic user client credentials configuration
            AuthorizationHeaderProviderOptions options = new()
            {
                AcquireTokenOptions = new AcquireTokenOptions
                {
                    AuthenticationOptionsName = "MsalAgent"
                }
            };

            // Use the Microsoft.Identity.Web boundary API to acquire token with agentic user.
            options.WithAgentUserIdentity(agenticUser.AgenticAppInstanceId, Guid.Parse(agenticUser.AgenticUserId));

            return await _authorizationHeaderProvider.CreateAuthorizationHeaderAsync(
                [scope],
                options,
                null,
                cancellationToken);
        }
    }
}
