// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;

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
                    await turnContext.SendActivityAsync(MessageFactory.Text("Welcome to AuthenticationBot. Type anything to get logged in. Type 'logout' to sign-out."), cancellationToken);
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

            await base.OnMessageActivityAsync(turnContext, cancellationToken);
        }

        protected override async Task OnTeamsSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running dialog with sign-in/verify state from an Invoke Activity.");

            // The OAuth Prompt needs to see the Invoke Activity in order to complete the login process.
            // Run the Dialog with the new Invoke Activity.
            await _dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }
    }
}
