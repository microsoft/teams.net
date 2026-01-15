// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Core.Schema;
using Microsoft.Teams.BotApps;

namespace Microsoft.Bot.Core.Compat;

internal sealed class CompatAdapterMiddleware(IMiddleware bfMiddleWare) : ITurnMiddleWare
{
    public Task OnTurnAsync(BotApplication botApplication, CoreActivity activity, NextTurn nextTurn, CancellationToken cancellationToken = default)
    {

        if (botApplication is TeamsBotApplication tba)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            TurnContext turnContext = new(new CompatBotAdapter(tba), activity.ToCompatActivity());
#pragma warning restore CA2000 // Dispose objects before losing scope

            turnContext.TurnState.Add<Connector.Authentication.UserTokenClient>(
                new CompatUserTokenClient(botApplication.UserTokenClient)
            );

            turnContext.TurnState.Add<Connector.IConnectorClient>(
                new CompatConnectorClient(
                    new CompatConversations(botApplication.ConversationClient)
                    {
                        ServiceUrl = activity.ServiceUrl?.ToString()
                    }
                )
            );

            return bfMiddleWare.OnTurnAsync(turnContext, (activity)
                    => nextTurn(cancellationToken), cancellationToken);
        }
        return Task.CompletedTask;
    }

}
