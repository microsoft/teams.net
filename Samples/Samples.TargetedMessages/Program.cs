using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams().AddTeamsDevTools();
var app = builder.Build();
var teams = app.UseTeams();

// Log all incoming activities
teams.OnActivity(async context =>
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

    if (text.Contains("send"))
    {
        // SEND: Create a new targeted message
        await context.Send(
            new MessageActivity("👋 This is a **targeted message** - only YOU can see this!")
                .WithTargetedRecipient(true), cancellationToken);
        
        context.Log.Info($"[SEND] Sent targeted message");
    }
    else if (text.Contains("update"))
    {
        // UPDATE: Send a targeted message, then update it after 3 seconds
        var conversationId = activity.Conversation?.Id ?? "";
        var userId = activity.From?.Id ?? "";

        var response = await context.Send(
            new MessageActivity("📝 This message will be **updated** in 3 seconds...")
                .WithTargetedRecipient(true), cancellationToken);
        
        if (response?.Id != null)
        {
            var messageId = response.Id;

            _ = Task.Run(async () =>
            {
                await Task.Delay(3000);

                try
                {
                    var updatedMessage = new MessageActivity($"✏️ **Updated!** This message was modified at {DateTime.UtcNow:HH:mm:ss}")
                        .WithTargetedRecipient(userId);

                    await context.Api.Conversations.Activities.UpdateTargetedAsync(conversationId, messageId, updatedMessage);

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
    else if (text.Contains("delete"))
    {
        // DELETE: Send a targeted message, then delete it after 3 seconds
        var conversationId = activity.Conversation?.Id ?? "";

        var response = await context.Send(
            new MessageActivity("🗑️ This message will be **deleted** in 3 seconds...")
                .WithTargetedRecipient(true), cancellationToken);
        
        if (response?.Id != null)
        {
            var messageId = response.Id;

            _ = Task.Run(async () =>
            {
                await Task.Delay(3000);

                try
                {
                    await context.Api.Conversations.Activities.DeleteTargetedAsync(conversationId, messageId);

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
    else if (text.Contains("reply"))
    {
        // REPLY: Send a targeted reply to the user's message
        await context.Reply(
            new MessageActivity("💬 This is a **targeted reply** - threaded and private!")
                .WithTargetedRecipient(true), cancellationToken);
        
        context.Log.Info("[REPLY] Sent targeted reply");
    }
    else if (text.Contains("help"))
    {
        await context.Send(
            "**🎯 Targeted Messages Demo**\n\n" +
            "**Commands:**\n" +
            "- `send` - Send a targeted message (only you see it)\n" +
            "- `update` - Send a message, then update it after 3 seconds\n" +
            "- `delete` - Send a message, then delete it after 3 seconds\n" +
            "- `reply` - Get a targeted reply (threaded)\n\n" +
            "_Targeted messages are only visible to you, even in group chats!_", cancellationToken
        );
    }
    else
    {
        await context.Typing();
        await context.Send($"You said: '{activity.Text}'\n\nType `help` to see available commands.", cancellationToken);
    }
});

app.Run();