// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Compat;

internal sealed class CompatAdapterMiddleware(IMiddleware bfMiddleWare) : ITurnMiddleWare
{
    public Task OnTurnAsync(BotApplication botApplication, CoreActivity activity, NextTurn nextTurn, CancellationToken cancellationToken = default)
    {

        if (botApplication is TeamsBotApplication tba)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            TurnContext turnContext = new(new CompatBotAdapter(tba), activity.ToCompatActivity());
#pragma warning restore CA2000 // Dispose objects before losing scope

            turnContext.TurnState.Add<Microsoft.Bot.Connector.Authentication.UserTokenClient>(
                new CompatUserTokenClient(botApplication.UserTokenClient)
            );

            turnContext.TurnState.Add<Microsoft.Bot.Connector.IConnectorClient>(
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
