using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams().AddTeamsDevTools();
var app = builder.Build();
var teams = app.UseTeams();

teams.OnMessage(async (context, cancellationToken) =>
{
    var text = (context.Activity.Text ?? "").ToLowerInvariant();
    var conversationId = context.Ref.Conversation.Id;
    var messageId = context.Activity.Id!;

    // When inside a thread, conversationId contains ;messageid=<rootId>.
    // Extract the root ID for threading; for top-level messages, use activity.id.
    var threadParts = conversationId.Split(";messageid=");
    var threadRootId = threadParts.Length > 1 ? threadParts[1] : messageId;

    // ============================================
    // context.Reply() — reactive threaded reply
    // ============================================
    if (text.Contains("test reply"))
    {
        await context.Reply("This is a threaded reply to your message.", cancellationToken);
        return;
    }

    // ============================================
    // context.Send() — reactive send to same thread
    // ============================================
    if (text.Contains("test send"))
    {
        await context.Send("This is sent to the same thread, without quoting.", cancellationToken);
        return;
    }

    // ============================================
    // teams.Reply() — proactive threaded reply
    // ============================================
    if (text.Contains("test proactive"))
    {
        await teams.Reply(conversationId, threadRootId, "This is a proactive threaded reply using teams.Reply().", cancellationToken);
        return;
    }

    // ============================================
    // ToThreadedConversationId() + teams.Send() — advanced manual control (channels and 1:1 chats only)
    // ============================================
    if (text.Contains("test manual"))
    {
        // ToThreadedConversationId() is only valid for conversations that support threading
        var baseId = conversationId.Split(';')[0];
        if (!baseId.EndsWith("@thread.tacv2") && !baseId.EndsWith("@thread.skype") && !baseId.EndsWith("@unq.gbl.spaces"))
        {
            await context.Reply("This command doesn't support threading in this conversation type.", cancellationToken);
            return;
        }
        var threadId = Microsoft.Teams.Api.Conversation.ToThreadedConversationId(conversationId, threadRootId);
        await teams.Send(threadId, "This was sent using ToThreadedConversationId() + teams.Send() for manual control.", cancellationToken: cancellationToken);
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
            "- `test proactive` - teams.Reply() proactive threaded reply\n" +
            "- `test manual` - ToThreadedConversationId() + teams.Send() for advanced control",
            cancellationToken);
        return;
    }

    await context.Send("Say \"help\" for available commands.", cancellationToken);
});

app.Run();
