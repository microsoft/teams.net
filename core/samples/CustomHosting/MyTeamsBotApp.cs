// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.Handlers;

namespace CustomHosting;

public class MyTeamsBotApp : TeamsBotApplication
{
    public MyTeamsBotApp(
        ApiClient api,
        IHttpContextAccessor accessor,
        ILogger<MyTeamsBotApp> logger,
        TeamsBotApplicationOptions? options = null)
        : base(api, accessor, logger, options)
    {
        this.OnMessage(async (ctx, ct) =>
        {
            await ctx.SendActivityAsync("Hello from MyTeamsBotApp!", ct);
        });
    }
}
