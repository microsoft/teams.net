// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

// Send a targeted message (visible only to the inbound sender).
// Use WithRecipient(account, isTargeted: true) on the builder, then send.
teamsApp.OnMessage("(?i)^test send$", async (context, cancellationToken) =>
{
    TeamsActivity reply = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityType.Message)
        .WithText("👋 Only you can see this targeted message.")
        .WithRecipient(context.Activity.From, isTargeted: true)
        .Build();
    await context.SendActivityAsync(reply, cancellationToken);
});

// Targeted reply to the inbound message: same wire format as send, but goes through
// Context.Reply which prepends a quoted reference to the inbound message.
teamsApp.OnMessage("(?i)^test reply$", async (context, cancellationToken) =>
{
    TeamsActivity reply = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityType.Message)
        .WithText("🔒 Targeted reply visible only to you.")
        .WithRecipient(context.Activity.From, isTargeted: true)
        .Build();
    await context.Reply(reply, cancellationToken);
});

// Send → Update a targeted message after 3 seconds.
teamsApp.OnMessage("(?i)^test update$", async (context, cancellationToken) =>
{
    string conversationId = context.Activity.Conversation?.Id ?? string.Empty;

    TeamsActivity initial = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityType.Message)
        .WithText("📝 This targeted message will be updated in 3 seconds…")
        .WithRecipient(context.Activity.From, isTargeted: true)
        .Build();

    SendActivityResponse? response = await context.SendActivityAsync(initial, cancellationToken);

    if (response?.Id is null) return;

    string messageId = response.Id;
    _ = Task.Run(async () =>
    {
        await Task.Delay(3000);
        try
        {
            MessageActivity updated = new($"✏️ Updated at {DateTime.UtcNow:HH:mm:ss}");
            await context.Api.Conversations.Activities.UpdateTargetedAsync(conversationId, messageId, updated, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UPDATE] error: {ex.Message}");
        }
    });
});

// Send → Delete a targeted message after 3 seconds.
teamsApp.OnMessage("(?i)^test delete$", async (context, cancellationToken) =>
{
    string conversationId = context.Activity.Conversation?.Id ?? string.Empty;

    TeamsActivity initial = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityType.Message)
        .WithText("🗑️ This targeted message will be deleted in 3 seconds…")
        .WithRecipient(context.Activity.From, isTargeted: true)
        .Build();

    SendActivityResponse? response = await context.SendActivityAsync(initial, cancellationToken);

    if (response?.Id is null) return;

    string messageId = response.Id;
    _ = Task.Run(async () =>
    {
        await Task.Delay(3000);
        try
        {
            await context.Api.Conversations.Activities.DeleteTargetedAsync(conversationId, messageId, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DELETE] error: {ex.Message}");
        }
    });
});

// Help
teamsApp.OnMessage("(?i)^help$", async (context, cancellationToken) =>
{
    await context.SendActivityAsync(
        new MessageActivity(
            "**Targeted Messages Test Bot**\n\n" +
            "**Commands:**\n" +
            "- `test send` — Send a targeted message (visible only to you)\n" +
            "- `test reply` — Reply with a targeted message\n" +
            "- `test update` — Send then update a targeted message\n" +
            "- `test delete` — Send then delete a targeted message\n")
        { TextFormat = TextFormats.Markdown },
        cancellationToken);
});

webApp.Run();
