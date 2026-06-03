// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);

// Register the Teams bot with per-turn state backed by Redis.
// Swap AddStackExchangeRedisCache for AddDistributedMemoryCache() during local dev
// if you don't have a Redis instance available.
webAppBuilder.Services.AddTeamsBotApplication(options => options.WithState());
//webAppBuilder.Services.AddDistributedMemoryCache();
webAppBuilder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = webAppBuilder.Configuration.GetConnectionString("Redis") ?? throw new InvalidProgramException("Redis connection string not found");
});

WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

teamsApp.OnMessage(async (ctx, ct) =>
{
    // Conversation state: shared across all users in the conversation
    int convCounter = ctx.State.ConversationState.Get<int>("counter");
    convCounter++;
    ctx.State.ConversationState.Set("counter", convCounter);

    // User state: private to each user in the conversation
    int userCounter = ctx.State.UserState!.Get<int>("counter");
    userCounter++;
    ctx.State.UserState.Set("counter", userCounter);

    await ctx.SendActivityAsync(
        $"Conversation message #{convCounter}, your message #{userCounter}.", ct);
});

webApp.Run();
