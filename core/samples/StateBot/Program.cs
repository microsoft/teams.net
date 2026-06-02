// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This sample demonstrates turn state: the three scopes (conversation, user, temp), automatic
// save-on-success, and delete-on-clear. State is opt-in — register a store with UseState and read
// it through context.State during a turn.

using System.Text.RegularExpressions;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.State;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);

webAppBuilder.Services.AddTeamsBotApplication(options =>
{
    // Opt in to state with an in-process store. Swap for `new FileStorage("./bot-state")` to persist
    // across restarts, or a distributed store (e.g. Redis) for multi-instance deployments.
    options.UseState(new MemoryStorage());
});

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
        - `note <text>` — stash a value in **temp** state (never persisted — gone next turn)
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

// ==================== TEMP SCOPE ====================

// Never persisted: useful for passing data between middleware and handlers within one turn.
bot.OnMessage("(?i)^note (.+)$", async (context, ct) =>
{
    Match match = Regex.Match(context.Activity.Text ?? "", "(?i)^note (.+)$");
    context.State!.Temp.Set("note", match.Groups[1].Value.Trim());

    string? echo = context.State.Temp.Get<string>("note");
    await context.SendActivityAsync(
        $"Stashed `{echo}` in temp state — for this turn only. It won't survive to the next message.", ct);
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
