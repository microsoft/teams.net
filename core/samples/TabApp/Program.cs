// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Schema;
using TabApp;

var builder = TeamsBotApplication.CreateBuilder(args);

var app = builder.Build();

// Serve the React tab at /tabs/test (build the web app first: cd Web && npm install && npm run build)
app.WithTab("test", "./Web/bin");

// ==================== SERVER FUNCTIONS ====================

app.WithFunction<PostToChatBody>("post-to-chat", async (ctx, ct) =>
{
    ctx.Log.LogInformation("post-to-chat called by {User} with message: {Message}", ctx.UserName, ctx.Data.Message);
    await ctx.SendAsync(ctx.Data.Message, ct);
    return new { ok = true };
});

// TODO: Once SSO is implemented, review moving who-am-i and toggle-status functions to server side

// ==================== MESSAGE ====================
app.OnMessage(async (ctx, ct) =>
{
    await ctx.SendActivityAsync(
        new MessageActivity("Open the **Tab** tab to interact with the sample."),
        ct);
});

app.Run();
