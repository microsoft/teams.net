// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);

// Register the Teams bot with per-turn state backed by Redis.
// Remove the AddStackExchangeRedisCache block below to use the built-in
// in-memory fallback during local dev.
webAppBuilder.Services.AddTeamsBotApplication(options => options.UseState());
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
    await ctx.SendAsync($"Message #{counter} in this conversation.", ct);

    UserPrefs up = ctx.State.UserState?.Get<UserPrefs>() ?? new UserPrefs();
    await ctx.SendAsync($"Your name is {up.UserName} and your favorite color is {up.FavoriteColor}.", ct);

    up.UserName = "User" + counter;
    up.FavoriteColor = Colors[counter % Colors.Length];
    ctx.State.UserState?.Set(up);
});

webApp.Run();

static partial class Program
{
    internal static readonly string[] Colors = ["Red", "Blue", "Green", "Yellow", "Purple", "Orange", "Cyan", "Magenta"];
}

class UserPrefs
{
    public string FavoriteColor { get; set; } = "White";
    public string UserName { get; set; } = "anon";
}
