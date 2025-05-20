using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Samples.BotBuilder.Bot
{
    public class Bot : ActivityHandler
    {
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var replyText = $"hi from botbuilder...";
            await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
        }
    }
}