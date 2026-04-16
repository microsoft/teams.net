// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;

namespace MigrationBot;

/// <summary>
/// Teams SDK–style echo bot — the migration target.
///
/// This is a dedicated <see cref="TeamsBotApplication"/> subclass registered
/// independently from the CompatAdapter's own <see cref="TeamsBotApplication"/>.
/// Each instance owns its own isolated router and OnActivity delegate, so the two
/// instances never interfere with each other's routing.
///
/// As handlers are moved here from <see cref="LegacyEchoBot"/>, remove them from
/// that class.  When <see cref="LegacyEchoBot"/> is empty, drop the CompatAdapter
/// and the <c>Migration:UseTeamsSdk</c> flag and register this class directly as a
/// plain <see cref="TeamsBotApplication"/>.
/// </summary>
public class NewSdkBot : TeamsBotApplication
{
    public NewSdkBot(
        ConversationClient conversationClient,
        UserTokenClient userTokenClient,
        TeamsApiClient teamsApiClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TeamsBotApplication> logger,
        BotApplicationOptions? options = null)
        : base(conversationClient, userTokenClient, teamsApiClient, httpContextAccessor, logger, options)
    {
        // ── Handler: message activity ─────────────────────────────────────────
        // Equivalent in LegacyEchoBot:
        //   OnMessageActivityAsync → MessageFactory.Text($"Echo (Bot Framework): {text}")
        this.OnMessage(async (context, cancellationToken) =>
        {
            await context.SendActivityAsync(
                $"Echo (Teams SDK): {context.Activity.Text}", cancellationToken);
        });

        // ── Handler: members added ────────────────────────────────────────────
        // Equivalent in LegacyEchoBot:
        //   OnMembersAddedAsync → MessageFactory.Text("Welcome! ... CompatAdapter.")
        this.OnMembersAdded(async (context, cancellationToken) =>
        {
            await context.SendActivityAsync(
                "Welcome! This bot is powered by the Teams SDK.", cancellationToken);
        });
    }
}
