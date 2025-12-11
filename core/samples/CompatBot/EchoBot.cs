// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;

namespace CompatBot;

public class ConversationData
{
    public int MessageCount { get; set; } = 0;

}

internal class EchoBot(ConversationState conversationState) : TeamsActivityHandler
{
    public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
    {
        await base.OnTurnAsync(turnContext, cancellationToken);

        await conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        IStatePropertyAccessor<ConversationData> conversationStateAccessors = conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
        ConversationData conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData(), cancellationToken);

        string replyText = $"Echo from BF Compat [{conversationData.MessageCount++}]: {turnContext.Activity.Text}";
        await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
        await turnContext.SendActivityAsync(MessageFactory.Text($"[Send a proactive message `/api/notify/{turnContext.Activity.Conversation.Id}`"), cancellationToken);
    }

    protected override async Task OnMessageReactionActivityAsync(ITurnContext<IMessageReactionActivity> turnContext, CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync(MessageFactory.Text("Message reaction received."), cancellationToken);
    }
}
