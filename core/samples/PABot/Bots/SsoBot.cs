// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace PABot.Bots
{
    public class SsoBot(ILogger<SsoBot> logger) : ActivityHandler
    {
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(MessageFactory.Text($"Echo: {turnContext.Activity.Text}"), cancellationToken);

            UserTokenClient utc = turnContext.TurnState.Get<UserTokenClient>();

            TokenStatus[] tokenStatus = await utc.GetTokenStatusAsync(turnContext.Activity.From.Id, turnContext.Activity.ChannelId, string.Empty, cancellationToken);

            logger.LogInformation("Token status count");
            //logger.LogInformation(JsonConvert.SerializeObject(tokenStatus));
            await turnContext.SendActivityAsync($"Token status count: {tokenStatus.Length}");

            foreach (var ts in tokenStatus)
            {
                if (ts.HasToken == true)
                {
                    var tokenResponse = await utc.GetUserTokenAsync(turnContext.Activity.From.Id, turnContext.Activity.ChannelId, ts.ConnectionName, null, cancellationToken);
                    //logger.LogInformation("Token for connection '{ConnectionName}': {Token}", ts.ConnectionName, tokenResponse?.Token);
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Token for connection '{ts.ConnectionName}': {tokenResponse?.Token}"), cancellationToken);
                }
                else
                {
                    //logger.LogInformation("No token for connection '{ConnectionName}'", ts.ConnectionName);
                    await turnContext.SendActivityAsync(MessageFactory.Text($"No token for connection '{ts.ConnectionName}'"), cancellationToken);

                    Activity? a = turnContext.Activity as Activity;
                    var signInResource = await utc.GetSignInResourceAsync(ts.ConnectionName, a, string.Empty, cancellationToken);
                    //logger.LogInformation("Sign-in resource for connection '{ConnectionName}': {SignInLink}", ts.ConnectionName, signInResource.SignInLink);
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Sign-in resource for connection '{ts.ConnectionName}': {signInResource.SignInLink}"), cancellationToken);

                }
            }


        }
    }
}
