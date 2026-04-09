// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using System.Net;

namespace PABot.Bots
{
    /// <summary>
    /// OAuth SSO Bot - Demonstrates OAuth Single Sign-On flow with token exchange.
    ///
    /// Flow:
    /// 1. User sends any message
    /// 2. Bot checks if user has a token
    /// 3. If no token, sends OAuth SSO card with TokenExchangeResource
    /// 4. Client attempts SSO token exchange by sending invoke activity
    /// 5. Bot handles token exchange and responds with success/failure
    /// 6. If token exchange fails, user clicks sign-in button for manual auth
    /// </summary>
    public class SsoBot(ILogger<SsoBot> logger, IConfiguration configuration) : ActivityHandler
    {
        private readonly IConfiguration _configuration = configuration;
        private const string ConnectionName = "graph-sso"; // From launchSettings.json

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Special test scenario to reproduce NullReferenceException bug
            if (turnContext.Activity.Text?.Contains("test oauth card", StringComparison.OrdinalIgnoreCase) == true)
            {
                await TestOAuthCardSendScenario(turnContext, cancellationToken);
                return;
            }

            UserTokenClient tokenClient = turnContext.TurnState.Get<UserTokenClient>();

            // Try to get existing token
            TokenResponse token = await tokenClient.GetUserTokenAsync(
                turnContext.Activity.From.Id,
                ConnectionName,
                turnContext.Activity.ChannelId,
                null,
                cancellationToken);

            if (token != null && !string.IsNullOrEmpty(token.Token))
            {
                // User is authenticated - show token info
                logger.LogInformation("User has valid token for connection '{ConnectionName}'", ConnectionName);
                await turnContext.SendActivityAsync(
                    MessageFactory.Text($"✅ You are signed in!\n\nToken (first 20 chars): {token.Token[..Math.Min(20, token.Token.Length)]}...\n\nYou said: {turnContext.Activity.Text}"),
                    cancellationToken);
            }
            else
            {
                // No token - send OAuth SSO card
                logger.LogInformation("No token found, sending OAuth SSO card");
                await SendOAuthCardAsync(turnContext, cancellationToken);
            }
        }

        protected override async Task<InvokeResponse> OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            logger.LogInformation("Received invoke activity: {Name}", turnContext.Activity.Name);

            // Handle token exchange invoke (SSO)
            if (turnContext.Activity.Name == SignInConstants.TokenExchangeOperationName)
            {
                return await OnTokenExchangeInvokeAsync(turnContext, cancellationToken);
            }

            // Handle signin verification invoke (manual sign-in fallback)
            if (turnContext.Activity.Name == SignInConstants.VerifyStateOperationName)
            {
                return await OnVerifyStateInvokeAsync(turnContext, cancellationToken);
            }

            // Let base class handle other invokes (like Teams-specific invokes)
            return await base.OnInvokeActivityAsync(turnContext, cancellationToken);
        }

        /// <summary>
        /// Sends an OAuth SSO card with TokenExchangeResource to enable SSO.
        /// </summary>
        private async Task SendOAuthCardAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            UserTokenClient tokenClient = turnContext.TurnState.Get<UserTokenClient>();

            // Get sign-in resource from token service (includes TokenExchangeResource for SSO)
            SignInResource signInResource = await tokenClient.GetSignInResourceAsync(
                ConnectionName,
                (Activity)turnContext.Activity,
                string.Empty,
                cancellationToken);

            logger.LogInformation("Got sign-in resource. SignInLink: {SignInLink}", signInResource.SignInLink);
            logger.LogInformation("TokenExchangeResource.Id: {Id}, Uri: {Uri}",
                signInResource.TokenExchangeResource?.Id,
                signInResource.TokenExchangeResource?.Uri);

            // Create OAuth SSO card
            var oAuthCard = new OAuthCard
            {
                Text = "Please sign in to continue",
                ConnectionName = ConnectionName,
                TokenExchangeResource = signInResource.TokenExchangeResource,
                TokenPostResource = signInResource.TokenPostResource,
                Buttons = new[]
                {
                    new CardAction
                    {
                        Title = "Sign In",
                        Text = "Sign in",
                        Type = ActionTypes.Signin,
                        Value = signInResource.SignInLink
                    }
                }
            };

            var reply = MessageFactory.Attachment(oAuthCard.ToAttachment());
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        /// <summary>
        /// Handles token exchange invoke for SSO.
        /// Client sends this invoke with a token it obtained, and the bot exchanges it for a token for the configured connection.
        /// </summary>
        private async Task<InvokeResponse> OnTokenExchangeInvokeAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            logger.LogInformation("Processing token exchange invoke");

            // Parse token exchange request from invoke value
            TokenExchangeInvokeRequest? tokenExchangeRequest = (turnContext.Activity.Value as JObject)?.ToObject<TokenExchangeInvokeRequest>();

            if (tokenExchangeRequest == null)
            {
                logger.LogWarning("Token exchange request is null");
                return CreateInvokeResponse(
                    HttpStatusCode.BadRequest,
                    new TokenExchangeInvokeResponse
                    {
                        Id = null,
                        ConnectionName = ConnectionName,
                        FailureDetail = "The bot received an InvokeActivity that is missing a TokenExchangeInvokeRequest value."
                    });
            }

            logger.LogInformation("Token exchange request - Id: {Id}, ConnectionName: {ConnectionName}",
                tokenExchangeRequest.Id, tokenExchangeRequest.ConnectionName);

            // Validate connection name matches
            if (tokenExchangeRequest.ConnectionName != ConnectionName)
            {
                logger.LogWarning("Connection name mismatch. Expected: {Expected}, Got: {Got}",
                    ConnectionName, tokenExchangeRequest.ConnectionName);
                return CreateInvokeResponse(
                    HttpStatusCode.BadRequest,
                    new TokenExchangeInvokeResponse
                    {
                        Id = tokenExchangeRequest.Id,
                        ConnectionName = ConnectionName,
                        FailureDetail = "The bot received a TokenExchangeInvokeRequest with a ConnectionName that does not match."
                    });
            }

            UserTokenClient tokenClient = turnContext.TurnState.Get<UserTokenClient>();

            // Attempt token exchange
            TokenResponse? tokenExchangeResponse = null;
            try
            {
                tokenExchangeResponse = await tokenClient.ExchangeTokenAsync(
                    turnContext.Activity.From.Id,
                    ConnectionName,
                    turnContext.Activity.ChannelId,
                    new TokenExchangeRequest { Token = tokenExchangeRequest.Token },
                    cancellationToken);

                logger.LogInformation("Token exchange result: {Success}",
                    tokenExchangeResponse != null && !string.IsNullOrEmpty(tokenExchangeResponse.Token) ? "Success" : "Failed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Token exchange failed with exception");
                // tokenExchangeResponse stays null
            }

            // Check if token exchange succeeded
            if (tokenExchangeResponse == null || string.IsNullOrEmpty(tokenExchangeResponse.Token))
            {
                logger.LogWarning("Token exchange failed - no token received");
                return CreateInvokeResponse(
                    HttpStatusCode.PreconditionFailed,
                    new TokenExchangeInvokeResponse
                    {
                        Id = tokenExchangeRequest.Id,
                        ConnectionName = ConnectionName,
                        FailureDetail = "Token exchange failed. The bot was unable to exchange the token."
                    });
            }

            // Success!
            logger.LogInformation("✅ Token exchange successful!");
            return CreateInvokeResponse(
                HttpStatusCode.OK,
                new TokenExchangeInvokeResponse
                {
                    Id = tokenExchangeRequest.Id,
                    ConnectionName = ConnectionName
                });
        }

        /// <summary>
        /// Handles signin verification invoke for manual sign-in fallback.
        /// When SSO token exchange fails, user clicks sign-in button and completes auth in browser.
        /// Teams then sends this invoke with a "magic code" that the bot must use to get the token.
        /// </summary>
        private async Task<InvokeResponse> OnVerifyStateInvokeAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            logger.LogInformation("Processing signin/verifyState invoke (manual sign-in fallback)");

            // Extract magic code from invoke value
            var magicCodeObject = turnContext.Activity.Value as JObject;
            var magicCode = magicCodeObject?.GetValue("state", StringComparison.Ordinal)?.ToString();

            if (string.IsNullOrEmpty(magicCode))
            {
                logger.LogWarning("Magic code is missing from signin/verifyState invoke");
                return CreateInvokeResponse(HttpStatusCode.BadRequest, null);
            }

            logger.LogInformation("Magic code received: {MagicCode}", magicCode[..Math.Min(10, magicCode.Length)] + "...");

            UserTokenClient tokenClient = turnContext.TurnState.Get<UserTokenClient>();

            // Getting the token follows a different flow in Teams. At the signin completion, Teams
            // will send the bot an "invoke" activity that contains a "magic" code. This code MUST
            // then be used to try fetching the token from Botframework service within some time
            // period. We try here. If it succeeds, we return 200 with an empty body. If it fails
            // with a retriable error, we return 500. Teams will re-send another invoke in this case.
            // If it fails with a non-retriable error, we return 404. Teams will not retry in that case.
            try
            {
                TokenResponse? token = await tokenClient.GetUserTokenAsync(
                    turnContext.Activity.From.Id,
                    ConnectionName,
                    turnContext.Activity.ChannelId,
                    magicCode,
                    cancellationToken);

                if (token != null && !string.IsNullOrEmpty(token.Token))
                {
                    logger.LogInformation("✅ Magic code verification successful! User is now signed in.");

                    // Success - return 200 with empty body
                    return CreateInvokeResponse(HttpStatusCode.OK, null);
                }
                else
                {
                    logger.LogWarning("Magic code verification failed - token is null or empty");

                    // Token is null - return 404, Teams will NOT retry
                    return CreateInvokeResponse(HttpStatusCode.NotFound, null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception during magic code verification");

                // Exception occurred - return 500, Teams WILL retry
                return CreateInvokeResponse(HttpStatusCode.InternalServerError, null);
            }
        }

        /// <summary>
        /// Creates an InvokeResponse with the specified status code and body.
        /// </summary>
        private static InvokeResponse CreateInvokeResponse(HttpStatusCode statusCode, object? body)
        {
            return new InvokeResponse
            {
                Status = (int)statusCode,
                Body = body
            };
        }

        /// <summary>
        /// Test scenario to reproduce the NullReferenceException issue when sending OAuth SSO cards.
        /// This mimics the ProjectAgentBot.SendOAuthCardForSSO scenario.
        /// </summary>
        private async Task TestOAuthCardSendScenario(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            try
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Testing OAuth SSO card send scenario..."), cancellationToken);

                // Get connector client - this uses CompatBotAdapter under the hood
                IConnectorClient connectorClient = turnContext.TurnState.Get<IConnectorClient>();

                if (connectorClient == null)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("❌ ERROR: ConnectorClient is null"), cancellationToken);
                    return;
                }

                // Get connection name from environment (set in launchSettings.json)
                string? connectionName = _configuration.GetValue<string>("ConnectionName");

                if (string.IsNullOrEmpty(connectionName))
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("❌ ERROR: ConnectionName not configured in launch profile"), cancellationToken);
                    return;
                }

                logger.LogInformation($"Creating SSO OAuth card with ConnectionName: {connectionName}");

                // Get UserTokenClient from TurnState (like the existing code does on line 29)
                UserTokenClient userTokenClient = turnContext.TurnState.Get<UserTokenClient>();

                // Get sign-in resource from token service
                SignInResource signInResource = await userTokenClient.GetSignInResourceAsync(
                    connectionName,
                    (Activity)turnContext.Activity,
                    string.Empty,
                    cancellationToken
                ).ConfigureAwait(false);

                logger.LogInformation($"Got sign-in resource from token service. SignInLink: {signInResource.SignInLink}");
                logger.LogInformation($"TokenExchangeResource: {signInResource.TokenExchangeResource?.Uri}");

                // Create proper SSO OAuth card exactly like OAuthPrompt does
                OAuthCard oAuthSsoCard = new OAuthCard
                {
                    Text = "Please sign in to continue",
                    ConnectionName = connectionName,
                    TokenExchangeResource = signInResource.TokenExchangeResource,
                    TokenPostResource = signInResource.TokenPostResource,
                    Buttons = new[]
                    {
                        new CardAction
                        {
                            Title = "Sign in",
                            Text = "Please sign in to continue",
                            Type = ActionTypes.Signin,
                            Value = signInResource.SignInLink
                        }
                    }
                };

                // Create activity using MessageFactory.Attachment - this is what ProjectAgentBot does
                IMessageActivity activity = MessageFactory.Attachment(oAuthSsoCard.ToAttachment());

                ((Microsoft.Bot.Schema.Activity)activity).AttachmentLayout = null;

                // Set properties like ProjectAgentBot does (lines 1255-1256)
                activity.Recipient = turnContext.Activity.From;  // Send to the user who messaged us
                activity.Conversation = turnContext.Activity.Conversation;

                logger.LogInformation("Sending OAuth card via connectorClient.Conversations.SendToConversationAsync");
                logger.LogInformation($"ConversationId: {activity.Conversation.Id}");
                logger.LogInformation($"Recipient: {activity.Recipient.Id}");

                // This is the call that causes NullReferenceException when APX returns 202 with empty body
                ResourceResponse response = await connectorClient.Conversations.SendToConversationAsync(
                    (Microsoft.Bot.Schema.Activity)activity,
                    cancellationToken
                );

                // If we get here, the call succeeded
                await turnContext.SendActivityAsync(MessageFactory.Text($"✅ SUCCESS! Response ID: {response?.Id ?? "NULL"}"), cancellationToken);
                logger.LogInformation($"OAuth card sent successfully. Response ID: {response?.Id}");
            }
            catch (NullReferenceException nre)
            {
                string errorMsg = $"❌ NullReferenceException caught! This is the bug we're investigating.\n" +
                                 $"Message: {nre.Message}\n" +
                                 $"StackTrace: {nre.StackTrace}";
                await turnContext.SendActivityAsync(MessageFactory.Text(errorMsg), cancellationToken);
                logger.LogError(nre, "NullReferenceException when sending OAuth card");
            }
            catch (Exception ex)
            {
                string errorMsg = $"❌ Unexpected exception: {ex.GetType().Name}\n" +
                                 $"Message: {ex.Message}\n" +
                                 $"StackTrace: {ex.StackTrace}";
                await turnContext.SendActivityAsync(MessageFactory.Text(errorMsg), cancellationToken);
                logger.LogError(ex, "Exception when sending OAuth card");
            }
        }
    }
}
