// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;

namespace MigrationBot;

/// <summary>
/// Bot Framework–style echo bot — the "legacy" side of the migration.
///
/// To migrate a handler to the Teams SDK:
///   1. Move the logic to the corresponding Teams SDK handler in Program.cs
///      (e.g. teamsApp.OnMessage / teamsApp.OnMembersAdded).
///   2. Delete the override from this class.
///
/// Once this class has no overrides left, remove it, unregister the IBot,
/// and drop the CompatAdapter (see Program.cs for full migration steps).
/// </summary>
internal class LegacyEchoBot : TeamsActivityHandler
{
    // ── Handler: message activity ─────────────────────────────────────────────
    // Migrated Teams SDK equivalent in Program.cs:
    //   teamsApp.OnMessage(async (context, ct) =>
    //       await context.SendActivityAsync($"Echo (Teams SDK): {context.Activity.Text}", ct));
    protected override async Task OnMessageActivityAsync(
        ITurnContext<IMessageActivity> turnContext,
        CancellationToken cancellationToken)
    {
        string replyText = $"Echo (Bot Framework): {turnContext.Activity.Text}";
        await turnContext.SendActivityAsync(MessageFactory.Text(replyText), cancellationToken);
    }

    // ── Handler: members added ────────────────────────────────────────────────
    // Migrated Teams SDK equivalent in Program.cs:
    //   teamsApp.OnMembersAdded(async (context, ct) =>
    //       await context.SendActivityAsync("Welcome! ...", ct));
    protected override async Task OnMembersAddedAsync(
        IList<ChannelAccount> membersAdded,
        ITurnContext<IConversationUpdateActivity> turnContext,
        CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync(
            MessageFactory.Text("Welcome! This bot is powered by Bot Framework via CompatAdapter."),
            cancellationToken);
    }
}
