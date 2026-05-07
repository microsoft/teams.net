// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
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

    // When inside a thread, conversationId contains ;messageid=<rootId>.
    // Extract the root ID for threading; for top-level messages, use activity.id.
    string[] threadParts = conversationId.Split(";messageid=");
    string threadRootId = threadParts.Length > 1 ? threadParts[1] : messageId;

    // ============================================
    // context.Reply() — reactive in-thread reply (sets ReplyToId)
    // ============================================
    if (text.Contains("test reply"))
    {
        await context.Reply("This is a reactive reply (in-thread, ReplyToId set).", cancellationToken);
        return;
    }

    // ============================================
    // context.Send() — reactive send to same conversation
    // ============================================
    if (text.Contains("test send"))
    {
        await context.Send("This is a reactive send (same conversation as the inbound).", cancellationToken);
        return;
    }

    // ============================================
    // teamsApp.Reply() — proactive threaded reply
    // ============================================
    if (text.Contains("test proactive"))
    {
        await teamsApp.Reply(conversationId, threadRootId, "This is a proactive threaded reply using teamsApp.Reply().", cancellationToken);
        return;
    }

    // ============================================
    // ToThreadedConversationId() + teamsApp.Send() — advanced manual control
    // ============================================
    if (text.Contains("test manual"))
    {
        string threadId = Conversation.ToThreadedConversationId(conversationId, threadRootId);
        await teamsApp.Send(threadId, "This was sent using ToThreadedConversationId() + teamsApp.Send() for manual control.", cancellationToken: cancellationToken);
        return;
    }

    // ============================================
    // Help / Default
    // ============================================
    if (text.Contains("help"))
    {
        await context.Reply(
            "**Threading Test Bot**\n\n" +
            "**Commands:**\n" +
            "- `test reply` - context.Reply() reactive threaded reply\n" +
            "- `test send` - context.Send() to same thread without quoting\n" +
            "- `test proactive` - teamsApp.Reply() proactive threaded reply\n" +
            "- `test manual` - ToThreadedConversationId() + teamsApp.Send() for advanced control",
            cancellationToken);
        return;
    }

    await context.Send("Say \"help\" for available commands.", cancellationToken);
});

webApp.Run();
