// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace PABot.Bots
{
    public class EchoBot(IHttpContextAccessor httpContextAccessor) : ActivityHandler
    {
        public override Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            string recipientId = turnContext.Activity.Recipient?.Id ?? string.Empty;
            string botId = recipientId.StartsWith("28:", StringComparison.Ordinal) ? recipientId[3..] : recipientId;

            httpContextAccessor.HttpContext?.SetBotId(botId);

            return base.OnTurnAsync(turnContext, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(MessageFactory.Text($"Echo from TurnContext.SendActivityAsync: {turnContext.Activity.Text}"), cancellationToken);

            IConnectorClient connectorClient = turnContext.TurnState.Get<IConnectorClient>();
            Activity activity = MessageFactory.Text($"Echo from IConversations.SendToConversationAsync: {turnContext.Activity.Text}");
            activity.Conversation = new ConversationAccount { Id = turnContext.Activity.Conversation.Id };
            await connectorClient.Conversations.SendToConversationAsync(activity);
        }
    }
}
