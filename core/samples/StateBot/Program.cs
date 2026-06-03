// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Drawing;
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
    int counter = ctx.State.ConversationState.Get<int>("counter");
    counter++;
    ctx.State.ConversationState.Set("counter", counter);
    await ctx.SendActivityAsync($"Message #{counter} in this conversation.", ct);

    UserPrefs up = ctx.State.UserState?.Get<UserPrefs>() ?? new UserPrefs();
    await ctx.SendActivityAsync($"Your name is {up.UserName} and your favorite color is {up.FavoriteColor}.", ct);

    up.UserName = "User" + counter;
    up.FavoriteColor = Color.FromArgb(counter % 256, (counter * 2) % 256, (counter * 3) % 256).Name;
    ctx.State.UserState?.Set(up);
});

webApp.Run();

class UserPrefs
{
    public string FavoriteColor { get; set; } = Color.White.Name;
    public string UserName { get; set; } = "anon";
}
