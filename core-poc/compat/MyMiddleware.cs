using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace compat;

public class MyMiddleware(ILogger logger) : Microsoft.Bot.Builder.IMiddleware
{
    public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("MyMiddleware executing.");

        turnContext.OnSendActivities(async (context, activities, nextSend) =>
        {
            bool containsMessage = activities.Any(e => e.Type == ActivityTypes.Message || e.Type == ActivityTypes.MessageUpdate);

            if (containsMessage)
            {
                await context.SendActivityAsync(new Activity(ActivityTypes.Typing)).ConfigureAwait(false);
            }

            return await nextSend().ConfigureAwait(false);
        });
        await next(cancellationToken).ConfigureAwait(false);
    }
}
