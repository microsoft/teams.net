// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace PABot.Bots
{
    public class SsoBot(ILogger<SsoBot> logger, IConfiguration configuration) : ActivityHandler
    {
        private readonly IConfiguration _configuration = configuration;
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Check if user is requesting the OAuth card test scenario
            if (turnContext.Activity.Text?.Contains("test oauth card", StringComparison.OrdinalIgnoreCase) == true)
            {
                await TestOAuthCardSendScenario(turnContext, cancellationToken);
                return;
            }

            await turnContext.SendActivityAsync(MessageFactory.Text($"Echo: {turnContext.Activity.Text}"), cancellationToken);

            UserTokenClient utc = turnContext.TurnState.Get<UserTokenClient>();

            TokenStatus[] tokenStatus = await utc.GetTokenStatusAsync(turnContext.Activity.From.Id, turnContext.Activity.ChannelId, string.Empty, cancellationToken);

            logger.LogInformation("Token status count");
            //logger.LogInformation(JsonConvert.SerializeObject(tokenStatus));
            await turnContext.SendActivityAsync($"Token status count: {tokenStatus.Length}");

            foreach (TokenStatus ts in tokenStatus)
            {
                if (ts.HasToken == true)
                {
                    TokenResponse tokenResponse = await utc.GetUserTokenAsync(turnContext.Activity.From.Id, turnContext.Activity.ChannelId, ts.ConnectionName, null, cancellationToken);
                    //logger.LogInformation("Token for connection '{ConnectionName}': {Token}", ts.ConnectionName, tokenResponse?.Token);
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Token for connection '{ts.ConnectionName}': {tokenResponse?.Token}"), cancellationToken);
                }
                else
                {
                    //logger.LogInformation("No token for connection '{ConnectionName}'", ts.ConnectionName);
                    await turnContext.SendActivityAsync(MessageFactory.Text($"No token for connection '{ts.ConnectionName}'"), cancellationToken);

                    Activity? a = turnContext.Activity as Activity;
                    SignInResource signInResource = await utc.GetSignInResourceAsync(ts.ConnectionName, a, string.Empty, cancellationToken);
                    //logger.LogInformation("Sign-in resource for connection '{ConnectionName}': {SignInLink}", ts.ConnectionName, signInResource.SignInLink);
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Sign-in resource for connection '{ts.ConnectionName}': {signInResource.SignInLink}"), cancellationToken);

                }
            }
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

                activity.ChannelId = turnContext.Activity.ChannelId; // Set channel ID

                activity.From = turnContext.Activity.Recipient; // Set 'from' as the bot (recipient of the incoming message)

                activity.ReplyToId = turnContext.Activity.Id; // Set ReplyToId to the incoming message ID

                activity.Locale = turnContext.Activity.Locale; // Set locale

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
