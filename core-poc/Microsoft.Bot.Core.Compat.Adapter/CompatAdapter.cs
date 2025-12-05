using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Core.Compat.Adapter;

public class CompatAdapter(BotApplication botApplication, CompatBotAdapter compatBotAdapter) : IBotFrameworkHttpAdapter
{
    public MiddlewareSet MiddlewareSet { get; } = new MiddlewareSet();

    public Func<ITurnContext, Exception, Task>? OnTurnError { get; set; }

    public CompatAdapter Use(Microsoft.Bot.Builder.IMiddleware middleware)
    {
        MiddlewareSet.Use(middleware);
        return this;
    }

    public async Task ProcessAsync(HttpRequest httpRequest, HttpResponse httpResponse, IBot bot, CancellationToken cancellationToken = default)
    {
        Microsoft.Bot.Core.Schema.CoreActivity? activity = null;
        botApplication.OnActivity = (activity, cancellationToken1) =>
        {
            TurnContext turnContext = new(compatBotAdapter, activity.ToCompatActivity());
            turnContext.TurnState.Add<Microsoft.Bot.Connector.Authentication.UserTokenClient>(new CompatUserTokenClient(botApplication.UserTokenClient));
            return bot.OnTurnAsync(turnContext, cancellationToken1);
        };
        try
        {
            foreach (Microsoft.Bot.Builder.IMiddleware? middleware in MiddlewareSet)
            {
                botApplication.Use(new CompatMiddlewareAdapter(middleware));
            }

            activity = await botApplication.ProcessAsync(httpRequest.HttpContext, cancellationToken);
        }
        catch (Exception ex)
        {
            if (OnTurnError != null)
            {
                if (ex is BotHanlderException aex)
                {
                    activity = aex.Activity;
                    TurnContext turnContext = new(compatBotAdapter, activity!.ToCompatActivity());
                    await OnTurnError(turnContext, ex);
                }
                else
                {
                    throw;
                }
            }
        }
    }

    public async Task ContinueConversationAsync(string botId, ConversationReference reference, BotCallbackHandler callback, CancellationToken cancellationToken)
    {
        TurnContext turnContext = new(compatBotAdapter, reference.GetContinuationActivity());
        await callback(turnContext, cancellationToken);
    }
}
