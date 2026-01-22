// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Compat;

/// <summary>
/// Adapts Bot Framework SDK middleware to work with the Teams Bot Core middleware pipeline.
/// </summary>
/// <remarks>
/// This adapter enables legacy Bot Framework middleware components to be used in the new Teams Bot Core architecture.
/// It converts CoreActivity instances to Bot Framework Activity format, creates appropriate turn contexts with
/// compatibility clients (UserTokenClient and ConnectorClient), and delegates processing to the Bot Framework middleware.
/// This allows gradual migration from Bot Framework SDK to Teams Bot Core while preserving existing middleware investments.
/// </remarks>
/// <param name="bfMiddleWare">The Bot Framework middleware component to adapt into the Teams Bot Core pipeline.</param>
internal sealed class CompatAdapterMiddleware(IMiddleware bfMiddleWare) : ITurnMiddleWare
{
    /// <summary>
    /// Processes a turn by converting the CoreActivity to Bot Framework format and invoking the wrapped middleware.
    /// </summary>
    /// <param name="botApplication">The bot application processing the turn. Must be a TeamsBotApplication instance.</param>
    /// <param name="activity">The activity to process in Core format.</param>
    /// <param name="nextTurn">A delegate to invoke the next middleware in the pipeline.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

            turnContext.TurnState.Add<Microsoft.Teams.Bot.Apps.TeamsApiClient>(tba.TeamsApiClient);

            return bfMiddleWare.OnTurnAsync(turnContext, (activity)
                    => nextTurn(cancellationToken), cancellationToken);
        }
        return Task.CompletedTask;
    }

}
