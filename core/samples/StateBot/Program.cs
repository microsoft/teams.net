// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;

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
ILogger logger = webApp.Logger;

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

teamsApp.OnMessage("(?i)^help$", async (context, ct) =>
{
    string helpText = """
        **StateBot commands**
        - `count` - increment conversation counter
        - `my name is <name>` - store your name in user state
        - `who am i` - read your name from user state
        - `show completed` - demo sealed state behavior after turn
        - `reset counter` - clear this conversation's state
        - `help` - show this message
        """;

    await context.SendAsync(
        MessageActivityInput.CreateBuilder().WithText(helpText, TextFormats.Markdown).Build(), ct);
});

// Persisted per conversation: shared by everyone in the chat.
teamsApp.OnMessage("(?i)^count$", async (context, ct) =>
{
    int count = context.State.ConversationState.Get<int>("count") + 1;
    context.State.ConversationState.Set("count", count);
    await context.SendAsync($"This conversation's counter is now **{count}**.", ct);
});

// Persisted per user in this conversation.
teamsApp.OnMessage("(?i)^my name is (.+)$", async (context, ct) =>
{
    string text = context.Activity.TextWithoutMentions?.Trim() ?? string.Empty;
    string name = text["my name is ".Length..].Trim();
    if (name.Length == 0)
    {
        await context.SendAsync("Please send `my name is <name>`.", ct);
        return;
    }

    context.State.UserState?.Set("name", name);
    await context.SendAsync($"Got it. I'll remember you as **{name}**.", ct);
});

teamsApp.OnMessage("(?i)^who am i$", async (context, ct) =>
{
    string? name = context.State.UserState?.Get<string>("name");
    if (string.IsNullOrWhiteSpace(name))
    {
        await context.SendAsync("I don't know yet. Tell me with `my name is <name>`.", ct);
        return;
    }

    await context.SendAsync(MessageActivityInput.CreateBuilder().WithText($"You are **{name}**.", TextFormats.Markdown).Build(), ct);
});

teamsApp.OnMessage("(?i)^show completed$", async (context, ct) =>
{
    var state = context.State;

    _ = Task.Run(async () =>
    {
        await Task.Delay(TimeSpan.FromSeconds(2));

        // IsCompleted lets background code observe that the turn ended without tripping the guard.
        bool completed = state.UserState?.IsCompleted ?? state.ConversationState.IsCompleted;
        if (completed)
        {
            try
            {
                _ = state.UserState?.Get<string>("name");
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning("Expected — state is sealed after the turn: {Message}", ex.Message);
            }
        }

    });

    await context.SendAsync("Started completion demo. Check logs in ~2 seconds.", ct);
});

teamsApp.OnMessage("(?i)^reset counter$", async (context, ct) =>
{
    context.State.ConversationState.Clear();
    await context.SendAsync("Cleared this conversation's state. The counter is back to zero.", ct);
});

webApp.Run();
