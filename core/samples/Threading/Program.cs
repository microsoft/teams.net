// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

teamsApp.OnMessage(async (context, cancellationToken) =>
{
    string text = (context.Activity.Text ?? "").ToLowerInvariant();
    string conversationId = context.Activity.Conversation!.Id;
    string messageId = context.Activity.Id!;

    // Store the agentic identity from the inbound activity's Recipient (the bot).
    // Required for proactive messaging when running with agentic identities.
    AgenticIdentity? agenticIdentity = context.Activity.Recipient?.GetAgenticIdentity();

    // When inside a thread, conversationId contains ;messageid=<rootId>.
    // Extract the root ID for threading; for top-level messages, use activity.id.
    string[] threadParts = conversationId.Split(";messageid=");
    string threadRootId = threadParts.Length > 1 ? threadParts[1] : messageId;

    // ============================================
    // context.ReplyAsync() — reactive in-thread reply (sets ReplyToId)
    // ============================================
    if (text.Contains("test reply"))
    {
        await context.ReplyAsync("This is a reactive reply (in-thread, ReplyToId set).", cancellationToken);
        return;
    }

    // ============================================
    // context.SendActivityAsync() — reactive send to same conversation
    // ============================================
    if (text.Contains("test send"))
    {
        await context.SendActivityAsync("This is a reactive send (same conversation as the inbound).", cancellationToken);
        return;
    }

    // ============================================
    // teamsApp.ReplyAsync() — proactive threaded reply
    // ============================================
    if (text.Contains("test proactive"))
    {
        await teamsApp.ReplyAsync(conversationId, threadRootId, "This is a proactive threaded reply using teamsApp.ReplyAsync().", agenticIdentity: agenticIdentity, cancellationToken: cancellationToken);
        return;
    }

    // ============================================
    // ConversationExtensions.ToThreadedConversationId() + teamsApp.SendAsync() — advanced manual control
    // ============================================
    if (text.Contains("test manual"))
    {
        string threadId = ConversationExtensions.ToThreadedConversationId(conversationId, threadRootId);
        await teamsApp.SendAsync(threadId, "This was sent using ToThreadedConversationId() + teamsApp.SendAsync() for manual control.", agenticIdentity: agenticIdentity, cancellationToken: cancellationToken);
        return;
    }

    // ============================================
    // Help / Default
    // ============================================
    if (text.Contains("help"))
    {
        MessageActivity helpMessage = new(
            "**Threading Test Bot**\n\n" +
            "**Commands:**\n" +
            "- `test reply` - context.ReplyAsync() reactive in-thread reply\n" +
            "- `test send` - context.SendActivityAsync() send to the same conversation\n" +
            "- `test proactive` - teamsApp.ReplyAsync() proactive threaded reply\n" +
            "- `test manual` - ToThreadedConversationId() + teamsApp.SendAsync() for advanced control")
        {
            TextFormat = TextFormats.Markdown
        };
        await context.ReplyAsync(helpMessage, cancellationToken);
        return;
    }

    await context.SendActivityAsync("Say \"help\" for available commands.", cancellationToken);
});

webApp.Run();
