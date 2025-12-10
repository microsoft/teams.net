using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
// using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace CompatBot;

public class ConversationData
{
    public int MessageCount { get; set; } = 0;

}

class EchoBot(ConversationState conversationState) : TeamsActivityHandler
{
    public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
    {
        await base.OnTurnAsync(turnContext, cancellationToken);

        await conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        var conversationStateAccessors = conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
        var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData(), cancellationToken);

        var replyText = $"Echo [{conversationData.MessageCount++}]: {turnContext.Activity.Text}";
        await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
    }

    protected override async Task OnMessageReactionActivityAsync(ITurnContext<IMessageReactionActivity> turnContext, CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync(MessageFactory.Text("Message reaction received."), cancellationToken);
    }
}