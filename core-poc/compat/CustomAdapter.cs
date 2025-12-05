
using Microsoft.Bot.Core;
using Microsoft.Bot.Core.Compat.Adapter;

namespace compat;

public class CustomAdapter : CompatAdapter
{
    public CustomAdapter(BotApplication botApplication, CompatBotAdapter compatBotAdapter, ILogger<CustomAdapter> logger) 
        : base(botApplication, compatBotAdapter)
    {
        Use(new MyMiddleware(logger));

        OnTurnError = async (turnContext, exception) =>
        {
            logger.LogError(exception, "[OnTurnError] unhandled error : {Message}", exception.Message);
            await turnContext.SendActivityAsync("The bot encountered an error or bug.");
            await turnContext.SendActivityAsync("To continue to run this bot, please fix the bot source code.");
        };
    }
}
