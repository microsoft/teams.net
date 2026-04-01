// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;

namespace CustomHosting;

public class MyTeamsBotApp : TeamsBotApplication
{
    public MyTeamsBotApp(ConversationClient conversationClient, UserTokenClient userTokenClient, TeamsApiClient teamsApiClient, IHttpContextAccessor httpContextAccessor, ILogger<TeamsBotApplication> logger, BotApplicationOptions? options = null) : base(conversationClient, userTokenClient, teamsApiClient, httpContextAccessor, logger, options)
    {
        this.OnMessage(async (ctx, ct) =>
        {
            await ctx.SendActivityAsync("Hello from MyTeamsBotApp!", ct);
        });
    }
}
