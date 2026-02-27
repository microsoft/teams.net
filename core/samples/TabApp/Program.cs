// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Schema;
using TabApp;

var builder = TeamsBotApplication.CreateBuilder(args);

// Serve the React tab at /tabs/test (build the web app first: cd Web && npm install && npm run build)
builder.WithTab("test", "./Web/bin");

// ==================== SERVER FUNCTIONS ====================

builder.WithFunction<PostToChatBody, PostToChatResult>("post-to-chat", async (ctx, ct) =>
{
    await ctx.SendAsync(ctx.Data?.Message?? "", ct);
    return new PostToChatResult(Ok: true);
});

// TODO: Once SSO is implemented, review moving who-am-i and toggle-status functions to server side

var app = builder.Build();

// ==================== MESSAGE ====================
app.OnMessage(async (ctx, ct) =>
{
    await ctx.SendActivityAsync(
        new MessageActivity("Open the **Tab** tab to interact with the sample."),
        ct);
});

app.Run();
