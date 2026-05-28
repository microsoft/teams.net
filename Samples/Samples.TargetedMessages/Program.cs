using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

#pragma warning disable ExperimentalTeamsTargeted

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams();
var app = builder.Build();
var teams = app.UseTeams();

// Log all incoming activities
teams.OnActivity(async (context, cancellationToken) =>
{
    context.Log.Info($"[ACTIVITY] Type: {context.Activity.Type}, From: {context.Activity.From?.Name ?? "unknown"}");
    await context.Next();
});

// Handle incoming messages
teams.OnMessage(async (context, cancellationToken) =>
{
    var activity = context.Activity;
    var text = activity.Text?.ToLowerInvariant() ?? "";

    context.Log.Info($"[MESSAGE] Received: {text}");

    if (text.Contains("test update"))
    {
        // UPDATE: Send a targeted message, then update it after 3 seconds
        var conversationId = activity.Conversation?.Id ?? "";

        var response = await context.Send(
            new MessageActivity("📝 This message will be **updated** in 3 seconds...")
                .WithRecipient(context.Activity.From, true), cancellationToken);
        
        if (response?.Id != null)
        {
            var messageId = response.Id;

            _ = Task.Run(async () =>
            {
                await Task.Delay(3000);

                try
                {
                    var updatedMessage = new MessageActivity($"✏️ **Updated!** This message was modified at {DateTime.UtcNow:HH:mm:ss}");

                    await context.Api.Conversations.Activities.UpdateTargetedAsync(conversationId, messageId, updatedMessage, cancellationToken);

                    Console.WriteLine($"[UPDATE] Updated targeted message");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UPDATE] Error: {ex.Message}");
                }
            });
        }

        context.Log.Info($"[UPDATE] Scheduled update in 3 seconds");
    }
    else if (text.Contains("test delete"))
    {
        // DELETE: Send a targeted message, then delete it after 3 seconds
        var conversationId = activity.Conversation?.Id ?? "";

        var response = await context.Send(
            new MessageActivity("🗑️ This message will be **deleted** in 3 seconds...")
                .WithRecipient(context.Activity.From, true), cancellationToken);
        
        if (response?.Id != null)
        {
            var messageId = response.Id;

            _ = Task.Run(async () =>
            {
                await Task.Delay(3000);

                try
                {
                    await context.Api.Conversations.Activities.DeleteTargetedAsync(conversationId, messageId, cancellationToken);

                    Console.WriteLine($"[DELETE] Deleted targeted message");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DELETE] Error: {ex.Message}");
                }
            });
        }

        context.Log.Info($"[DELETE] Scheduled delete in 3 seconds");
    }
    else if (text.Contains("test public"))
    {
        // PUBLIC: Send a public message visible to everyone in the chat.
        await context.Send(
            new MessageActivity("📋 Here is the public result — everyone can see this!"),
            cancellationToken);
        
        context.Log.Info("[PUBLIC] Sent public message");
    }
    else if (text.Contains("test send"))
    {
        // SEND: Send a targeted message visible only to the sender.
        await context.Send(
            new MessageActivity("👋 This is a **targeted message** — only YOU can see this!")
                .WithRecipient(context.Activity.From, true),
            cancellationToken);
        
        context.Log.Info("[SEND] Sent targeted message");
    }
    else if (text.Contains("help"))
    {
        await context.Send(
            "**🎯 Targeted Messages Demo**\n\n" +
            "**Commands:**\n" +
            "- `test send` - Send a targeted message (only visible to you)\n" +
            "- `test update` - Send a targeted message, then update it after 3 seconds\n" +
            "- `test delete` - Send a targeted message, then delete it after 3 seconds\n" +
            "- `test public` - Send a public reply (visible to all)\n\n" +
            "_Targeted messages are only visible to you, even in group chats!_", cancellationToken);
    }
    else
    {
        await context.Send($"You said: '{activity.Text}'\n\nType `help` to see available commands.", cancellationToken);
    }
});

app.Run();