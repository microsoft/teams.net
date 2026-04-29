// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Teams.Apps.BotBuilder;

namespace PABot.Bots
{
    /// <summary>
    /// This bot is derived from the TeamsActivityHandler class and handles Teams-specific activities.
    /// </summary>
    /// <typeparam name="T">The type of the dialog.</typeparam>
    public class TeamsBot<T> : DialogBot<T> where T : Dialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsBot{T}"/> class.
        /// </summary>
        /// <param name="conversationState">The conversation state.</param>
        /// <param name="userState">The user state.</param>
        /// <param name="dialog">The dialog.</param>
        /// <param name="logger">The logger.</param>
        public TeamsBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
            : base(conversationState, userState, dialog, logger)
        {
        }

        /// <summary>
        /// Handles the event when members are added to the conversation.
        /// </summary>
        /// <param name="membersAdded">The list of members added.</param>
        /// <param name="turnContext">The turn context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (ChannelAccount member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    string welcomeMessage = "Welcome to AuthenticationBot. Type anything to get logged in. Type 'logout' to sign-out.\n" +
                                          "Try these commands:\n" +
                                          "- /help - Show detailed help and bot capabilities\n" +
                                          "- /member-info - Get your member details\n" +
                                          "- /team-info - Get team details (in team context)\n" +
                                          "- /create-conversation - Create 1:1 chat (from group chat)";

                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeMessage), cancellationToken);

                    // Use TeamsApiClient.GetMemberAsync to get detailed member information
                    try
                    {
                        TeamsChannelAccount memberDetails = await TeamsApiClient.GetMemberAsync(turnContext, member.Id, cancellationToken);
                        _logger.LogInformation("Member added: {Name} ({Email}), AAD Object ID: {AadObjectId}",
                            memberDetails.Name, memberDetails.Email, memberDetails.AadObjectId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not retrieve member details for {MemberId}", member.Id);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Teams sign-in verification state.
        /// </summary>
        /// <param name="turnContext">The turn context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            string text = System.Text.RegularExpressions.Regex.Replace(
                turnContext.Activity.Text ?? string.Empty, @"<at>[^<]*<\/at>", string.Empty).Trim();

            if (text.Equals("/help", StringComparison.OrdinalIgnoreCase))
            {
                string helpMessage = "**PABot - Personal Assistant Bot**\n\n" +
                                   "I'm a Teams bot that demonstrates authentication and Teams integration capabilities.\n\n" +
                                   "**Authentication Features:**\n" +
                                   "- Dual OAuth authentication (graph and graph-2 connections)\n" +
                                   "- Sequential authentication flow - authenticate both connections one after another\n" +
                                   "- Retrieve and display user profile information from Microsoft Graph\n" +
                                   "- Show user profile photos\n" +
                                   "- Display OAuth tokens for both connections\n\n" +
                                   "**Available Commands:**\n" +
                                   "- **/help** - Show this help message\n" +
                                   "- **/member-info** - Get detailed information about your Teams member account\n" +
                                   "  - Displays: Name, ID, AAD Object ID, User Principal Name, Email\n" +
                                   "- **/team-info** - Get details about the current team (only works in team context)\n" +
                                   "  - Displays: Team Name, ID, AAD Group ID\n" +
                                   "- **/create-conversation** - Create a 1:1 conversation from a group chat\n" +
                                   "  - Only available in group chat contexts\n" +
                                   "- **logout** - Sign out from authenticated connections\n\n" +
                                   "**How to use:**\n" +
                                   "1. Send any message to start the authentication flow\n" +
                                   "2. Authenticate with the first connection (graph)\n" +
                                   "3. Authenticate with the second connection (graph-2)\n" +
                                   "4. View your profile information automatically\n" +
                                   "5. Use commands to explore Teams integration features\n\n" +
                                   "**Technical Features:**\n" +
                                   "- Uses TeamsApiClient for Teams member and team information\n" +
                                   "- Demonstrates Microsoft Graph integration\n" +
                                   "- Shows OAuth connection management\n" +
                                   "- Logs detailed member information when users join";

                await turnContext.SendActivityAsync(MessageFactory.Text(helpMessage), cancellationToken);
                return;
            }

            if (text.Equals("/create-conversation", StringComparison.OrdinalIgnoreCase))
            {
                if (turnContext.Activity.Conversation.IsGroup != true)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("This command can only be used in a group chat."), cancellationToken);
                    return;
                }

                TeamsChannelData channelData = turnContext.Activity.GetChannelData<TeamsChannelData>();
                ChannelAccount userChannel = turnContext.Activity.From;

                ConversationParameters conversationParameters = new ConversationParameters
                {
                    IsGroup = false,
                    Bot = new ChannelAccount { Id = turnContext.Activity.Recipient.Id },
                    Members = [userChannel],
                    TenantId = channelData.Tenant.Id,
                };

                _logger.LogInformation("Creating 1:1 conversation with user {UserId} in tenant {TenantId}",
                    userChannel.Id, conversationParameters.TenantId);

                IConnectorClient connectorClient = turnContext.TurnState.Get<IConnectorClient>();
                ConversationResourceResponse conv = await connectorClient.Conversations.CreateConversationAsync(conversationParameters, cancellationToken);

                _logger.LogInformation("Created conversation {ConversationId}", conv.Id);

                Activity message = MessageFactory.Text("Hello! I've started a 1:1 conversation with you from the group chat.");
                message.ServiceUrl = turnContext.Activity.ServiceUrl;
                await connectorClient.Conversations.SendToConversationAsync(conv.Id, message, cancellationToken);

                await turnContext.SendActivityAsync(MessageFactory.Text("Done! Check your personal chat."), cancellationToken);
                return;
            }

            if (text.Equals("/member-info", StringComparison.OrdinalIgnoreCase))
            {
                // Get member details using TeamsApiClient.GetMemberAsync
                string userId = turnContext.Activity.From.Id;
                TeamsChannelAccount member = await TeamsApiClient.GetMemberAsync(turnContext, userId, cancellationToken);

                string memberInfo = $"Member Details:\n" +
                                  $"- Name: {member.Name}\n" +
                                  $"- ID: {member.Id}\n" +
                                  $"- AAD Object ID: {member.AadObjectId}\n" +
                                  $"- User Principal Name: {member.UserPrincipalName}\n" +
                                  $"- Email: {member.Email}";

                await turnContext.SendActivityAsync(MessageFactory.Text(memberInfo), cancellationToken);
                return;
            }

            if (text.Equals("/team-info", StringComparison.OrdinalIgnoreCase))
            {
                // Get team details using TeamsApiClient.GetTeamDetailsAsync
                TeamInfo? teamInfo = turnContext.Activity.TeamsGetTeamInfo();
                if (teamInfo?.Id == null)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("This command can only be used in a team context."), cancellationToken);
                    return;
                }

                TeamDetails teamDetails = await TeamsApiClient.GetTeamDetailsAsync(turnContext, teamInfo.Id, cancellationToken);

                string teamDetailsInfo = $"Team Details:\n" +
                                       $"- Name: {teamDetails.Name}\n" +
                                       $"- ID: {teamDetails.Id}\n" +
                                       $"- AAD Group ID: {teamDetails.AadGroupId}";

                await turnContext.SendActivityAsync(MessageFactory.Text(teamDetailsInfo), cancellationToken);
                return;
            }

            await base.OnMessageActivityAsync(turnContext, cancellationToken);
        }

        protected override async Task OnTeamsSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running dialog with sign-in/verify state from an Invoke Activity.");

            // The OAuth Prompt needs to see the Invoke Activity in order to complete the login process.
            // Run the Dialog with the new Invoke Activity.
            await _dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }

        /// <summary>
        /// Handles invoke activities, including signin/failure events.
        /// </summary>
        /// <param name="turnContext">The turn context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An invoke response.</returns>
        protected override async Task<InvokeResponse> OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.Name == "signin/failure")
            {
                _logger.LogWarning("Sign-in failure detected. Activity Name: {ActivityName}", turnContext.Activity.Name);
                _logger.LogWarning("Sign-in failure details - ConversationId: {ConversationId}, From: {FromId}, Value: {Value}",
                    turnContext.Activity.Conversation?.Id,
                    turnContext.Activity.From?.Id,
                    Newtonsoft.Json.JsonConvert.SerializeObject(turnContext.Activity));
            }

            return await base.OnInvokeActivityAsync(turnContext, cancellationToken);
        }
    }
}
