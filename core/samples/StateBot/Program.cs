// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This sample demonstrates turn state: the conversation and user scopes, automatic save-on-success,
// delete-on-clear, and the after-turn guard (TurnState.IsCompleted) for fire-and-forget work. State is
// opt-in — enable it with UseState and read it through context.State during a turn.

using System.Text.RegularExpressions;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.State;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);

// Turn state defaults to an in-process cache (lost on restart). Set a "Redis" connection string —
// appsettings (ConnectionStrings:Redis) or the ConnectionStrings__Redis env var, e.g. "localhost:6379"
// — to back state with Redis instead, persisting it and sharing it across instances. Registering a
// distributed IDistributedCache here takes precedence over the in-process default.
string? redisConnection = webAppBuilder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    webAppBuilder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
}

// Opt in to state (uses Redis if configured above, otherwise the in-process default).
webAppBuilder.Services.AddTeamsBotApplication(options => options.UseState());

WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication bot = webApp.UseTeamsBotApplication();

// ==================== HELP ====================

bot.OnMessage("(?i)^help$", async (context, ct) =>
{
    const string help = """
        **State Bot** — demonstrates per-conversation and per-user state.

        Commands:
        - `count` — increment a counter in **conversation** state (shared by everyone in this chat)
        - `my name is <name>` — save your name in **user** state (follows you across conversations)
        - `whoami` — read your name back from user state
        - `remind me` — start fire-and-forget work that outlives the turn (shows `State.IsCompleted`)
        - `reset` — clear this conversation's state (deletes the stored document)
        - `help` — show this message
        """;

    await context.SendActivityAsync(new MessageActivity(help) { TextFormat = TextFormats.Markdown }, ct);
});

// ==================== CONVERSATION SCOPE ====================

// Persisted per conversation: every participant shares this counter, and it survives across turns.
bot.OnMessage("(?i)^count$", async (context, ct) =>
{
    StateScope conversation = context.State!.Conversation;
    int count = conversation.Get<int>("count") + 1;
    conversation.Set("count", count); // saved automatically when the turn completes

    await context.SendActivityAsync($"This conversation's counter is now **{count}**.", ct);
});

// ==================== USER SCOPE ====================

// Persisted per user across every conversation they're in.
bot.OnMessage("(?i)^my name is (.+)$", async (context, ct) =>
{
    Match match = Regex.Match(context.Activity.Text ?? "", "(?i)^my name is (.+)$");
    string name = match.Groups[1].Value.Trim();
    context.State!.User.Set("name", name);

    await context.SendActivityAsync($"Got it — I'll remember you as **{name}**.", ct);
});

bot.OnMessage("(?i)^whoami$", async (context, ct) =>
{
    string? name = context.State!.User.Get<string>("name");

    await context.SendActivityAsync(
        name is null ? "I don't know your name yet. Say `my name is <name>`." : $"You're **{name}**.", ct);
});

// ==================== FIRE-AND-FORGET: after-turn state access ====================

// TurnState is scoped to one turn: it is saved and then *sealed* when the handler returns, at which
// point TurnState.IsCompleted is true and any scope Get/Set throws. So for background work that
// outlives the turn, read the values you need DURING the turn and pass those in — never the live
// ctx.State, whose reads would otherwise be silently stale.
bot.OnMessage("(?i)^remind me$", async (context, ct) =>
{
    TurnState state = context.State!;

    // Capture what the background task needs NOW, while the turn is still active.
    string who = state.User.Get<string>("name") ?? "there";

    // Fire-and-forget: this runs after the handler returns (the turn is saved + sealed by then).
    _ = Task.Run(async () =>
    {
        await Task.Delay(TimeSpan.FromSeconds(2));

        // IsCompleted lets background code observe that the turn ended WITHOUT tripping the guard.
        if (state.IsCompleted)
        {
            // Reading the sealed scope throws — the guard turns a silent stale read into a loud error.
            // Use the value captured during the turn (`who`) instead of touching `state` here.
            try
            {
                _ = state.User.Get<string>("name");
            }
            catch (InvalidOperationException ex)
            {
                context.Log.Warn("Expected — state is sealed after the turn:", ex.Message);
            }
        }

        context.Log.Info("[background] reminder for", who, "(value captured during the turn).");
    });

    await context.SendActivityAsync(
        new MessageActivity(
            "Started background work. It checks `State.IsCompleted` (true once the turn ends) and uses a " +
            "value captured during the turn — reading the sealed `State` directly would throw.")
        {
            TextFormat = TextFormats.Markdown
        }, ct);
});

// ==================== CLEAR / DELETE ====================

// Clearing a persisted scope deletes its stored document (no empty row left behind).
bot.OnMessage("(?i)^reset$", async (context, ct) =>
{
    context.State!.Conversation.Clear();
    await context.SendActivityAsync("Cleared this conversation's state. The counter is back to zero.", ct);
});

// ==================== INSTALL ====================

bot.OnInstall(async (context, ct) =>
{
    await context.SendActivityAsync(
        new MessageActivity("Welcome to the **State Bot**! Type `help` to see what I can remember.")
        {
            TextFormat = TextFormats.Markdown
        }, ct);
});

webApp.Run();
