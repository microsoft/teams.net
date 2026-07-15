// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

// Send a targeted message (visible only to the inbound sender).
// Use WithRecipient(account, isTargeted: true) on the builder to stamp the targeted recipient;
// the send path detects the targeted recipient and routes it as a targeted message.
teamsApp.OnMessage("(?i)^test send$", async (context, cancellationToken) =>
{
    MessageActivityInput reply = MessageActivityInput.CreateBuilder()
        .WithText("👋 Only you can see this targeted message.")
        .WithRecipient(context.Activity.From!, isTargeted: true)
        .Build();
    await context.SendAsync(reply, cancellationToken);
});

// Targeted reply to the inbound message: same wire format as send, but goes through
// Context.Reply which prepends a quoted reference to the inbound message.
teamsApp.OnMessage("(?i)^test reply$", async (context, cancellationToken) =>
{
    MessageActivityInput reply = MessageActivityInput.CreateBuilder()
        .WithText("🔒 Targeted reply visible only to you.")
        .WithRecipient(context.Activity.From!, isTargeted: true)
        .Build();
    await context.ReplyAsync(reply, cancellationToken);
});

// Send → Update a targeted message after 3 seconds.
teamsApp.OnMessage("(?i)^test update$", async (context, cancellationToken) =>
{
    string conversationId = context.Activity.Conversation?.Id ?? string.Empty;

    MessageActivityInput initial = MessageActivityInput.CreateBuilder()
        .WithText("📝 This targeted message will be updated in 3 seconds…")
        .WithRecipient(context.Activity.From!, isTargeted: true)
        .Build();

    SendActivityResponse? response = await context.SendAsync(initial, cancellationToken);

    if (response?.Id is null) return;

    string messageId = response.Id;
    _ = Task.Run(async () =>
    {
        await Task.Delay(3000);
        try
        {
            MessageActivityInput updated = MessageActivityInput.CreateBuilder().WithText($"✏️ Updated at {DateTime.UtcNow:HH:mm:ss}").Build();
            await context.Api.Conversations.Activities.UpdateTargetedAsync(conversationId, messageId, updated, cancellationToken: cancellationToken);
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

    MessageActivityInput initial = MessageActivityInput.CreateBuilder()
        .WithText("🗑️ This targeted message will be deleted in 3 seconds…")
        .WithRecipient(context.Activity.From!, isTargeted: true)
        .Build();

    SendActivityResponse? response = await context.SendAsync(initial, cancellationToken);

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

// Detect whether the inbound message was itself a targeted message
// (i.e. sent only to the bot). Read it from context.Activity.Recipient?.IsTargeted.
// The reactive Prompt Preview auto-populate hook uses this same signal internally.
teamsApp.OnMessage("(?i)^test inbound$", async (context, cancellationToken) =>
{
    bool wasTargeted = context.Activity.Recipient?.IsTargeted == true;
    await context.SendAsync(
        wasTargeted
            ? "✅ Your message was delivered to me as a targeted message."
            : "ℹ️ Your message was delivered to me as a regular (broadcast) message.",
        cancellationToken);
});

// Help
teamsApp.OnMessage("(?i)^help$", async (context, cancellationToken) =>
{
    await context.SendAsync(
        MessageActivityInput.CreateBuilder()
            .WithText(
            "**Targeted Messages Test Bot**\n\n" +
            "**Commands:**\n" +
            "- `test send` — Send a targeted message (visible only to you)\n" +
            "- `test reply` — Reply with a targeted message\n" +
            "- `test update` — Send then update a targeted message\n" +
            "- `test delete` — Send then delete a targeted message\n" +
            "- `test inbound` — Show whether the inbound message was targeted at the bot\n", TextFormats.Markdown)
            .Build(),
        cancellationToken);
});

webApp.Run();
