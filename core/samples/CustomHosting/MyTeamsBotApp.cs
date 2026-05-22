// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;

namespace CustomHosting;

public class MyTeamsBotApp : TeamsBotApplication
{
    public MyTeamsBotApp(TeamsBotApplicationDependencies deps) : base(deps)
    {
        this.OnMessage(async (ctx, ct) =>
        {
            await ctx.SendActivityAsync("Hello from MyTeamsBotApp!", ct);
        });
    }
}
