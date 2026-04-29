// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Hosting;

namespace CustomHosting;

public class MyTeamsBotApp : TeamsBotApplication
{
    public MyTeamsBotApp(ConversationClient conversationClient, UserTokenClient userTokenClient, ApiClient teamsApiClient, IHttpContextAccessor httpContextAccessor, ILogger<TeamsBotApplication> logger, BotApplicationOptions? options = null) : base(conversationClient, userTokenClient, teamsApiClient, httpContextAccessor, logger, options)
    {
        this.OnMessage(async (ctx, ct) =>
        {
            await ctx.SendActivityAsync("Hello from MyTeamsBotApp!", ct);
        });
    }
}
