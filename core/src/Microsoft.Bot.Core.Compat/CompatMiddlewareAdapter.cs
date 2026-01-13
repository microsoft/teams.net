// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Core.Schema;

namespace Microsoft.Bot.Core.Compat;

internal sealed class CompatMiddlewareAdapter(IMiddleware bfMiddleWare) : ITurnMiddleWare
{
    public Task OnTurnAsync(BotApplication botApplication, CoreActivity activity, NextTurn nextTurn, CancellationToken cancellationToken = default)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        TurnContext turnContext = new(new CompatBotAdapter(botApplication), activity.ToCompatActivity());
#pragma warning restore CA2000 // Dispose objects before losing scope
        turnContext.TurnState.Add<Connector.Authentication.UserTokenClient>(new CompatUserTokenClient(botApplication.UserTokenClient));
        CompatConnectorClient connectionClient = new(new CompatConversations(botApplication.ConversationClient) { ServiceUrl = activity.ServiceUrl?.ToString() });
        turnContext.TurnState.Add<Microsoft.Bot.Connector.IConnectorClient>(connectionClient);
        return bfMiddleWare.OnTurnAsync(turnContext, (activity)
                => nextTurn(cancellationToken), cancellationToken);
    }
}
