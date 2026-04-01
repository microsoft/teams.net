// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace PABot.Bots
{
    public class EchoBot : ActivityHandler
    {
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
