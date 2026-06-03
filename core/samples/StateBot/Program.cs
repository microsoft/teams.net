// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);

// Register the Teams bot with per-turn state backed by Redis.
// Swap AddStackExchangeRedisCache for AddDistributedMemoryCache() during local dev
// if you don't have a Redis instance available.
webAppBuilder.Services.AddTeamsBotApplication(options => options.WithState());
webAppBuilder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = webAppBuilder.Configuration.GetConnectionString("Redis") ?? "localhost:6379,connectTimeout=3000,syncTimeout=3000";
});

WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

teamsApp.OnMessage(async (ctx, ct) =>
{
    int counter = ctx.State.Get<int>("counter");
    counter++;
    ctx.State.Set("counter", counter);
    await ctx.SendActivityAsync($"Message #{counter} in this conversation.", ct);
});

webApp.Run();
