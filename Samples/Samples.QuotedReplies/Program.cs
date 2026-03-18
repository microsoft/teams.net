using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Entities;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams().AddTeamsDevTools();
var app = builder.Build();
var teams = app.UseTeams();

teams.OnActivity(async context =>
{
    context.Log.Info($"[ACTIVITY] Type: {context.Activity.Type}, From: {context.Activity.From?.Name ?? "unknown"}");
    await context.Next();
});

teams.OnMessage(async (context, cancellationToken) =>
{
    var activity = context.Activity;
    var text = activity.Text?.ToLowerInvariant() ?? "";

    context.Log.Info($"[MESSAGE] Received: {text}");

    // ============================================
    // Read inbound quoted replies
    // ============================================
    var quotes = activity.GetQuotedMessages();
    if (quotes.Count > 0)
    {
        var quote = quotes[0].QuotedReply!;
        var info = $"Quoted message ID: {quote.MessageId}";
        if (quote.SenderName != null) info += $"\nFrom: {quote.SenderName}";
        if (quote.Preview != null) info += $"\nPreview: \"{quote.Preview}\"";
        if (quote.IsReplyDeleted == true) info += "\n(deleted)";
        if (quote.ValidatedMessageReference == true) info += "\n(validated)";

        await context.Send($"You sent a message with a quoted reply:\n\n{info}", cancellationToken);
    }

    // ============================================
    // Reply() — auto-quotes the inbound message
    // ============================================
    if (text.Contains("test reply"))
    {
        await context.Reply("This reply auto-quotes your message using Reply()", cancellationToken);
        return;
    }

    // ============================================
    // QuoteReply() — quote a previously sent message by ID
    // ============================================
    if (text.Contains("test quote"))
    {
        var sent = await context.Send("This message will be quoted next...", cancellationToken);
        await context.QuoteReply(sent.Id, "This quotes the message above using QuoteReply()", cancellationToken);
        return;
    }

    // ============================================
    // AddQuotedReply() — builder with response
    // ============================================
    if (text.Contains("test add"))
    {
        var sent = await context.Send("This message will be quoted next...", cancellationToken);
        var msg = new MessageActivity()
            .AddQuotedReply(sent.Id, "This uses AddQuotedReply() with a response");
        await context.Send(msg, cancellationToken);
        return;
    }

    // ============================================
    // Multi-quote interleaved
    // ============================================
    if (text.Contains("test multi"))
    {
        var sentA = await context.Send("Message A — will be quoted", cancellationToken);
        var sentB = await context.Send("Message B — will be quoted", cancellationToken);
        var msg = new MessageActivity()
            .AddQuotedReply(sentA.Id, "Response to A")
            .AddQuotedReply(sentB.Id, "Response to B");
        await context.Send(msg, cancellationToken);
        return;
    }

    // ============================================
    // AddQuotedReply() + AddText() — manual control
    // ============================================
    if (text.Contains("test manual"))
    {
        var sent = await context.Send("This message will be quoted next...", cancellationToken);
        var msg = new MessageActivity()
            .AddQuotedReply(sent.Id)
            .AddText(" Custom text after the quote placeholder");
        await context.Send(msg, cancellationToken);
        return;
    }

    // ============================================
    // ToQuoteReply() — obsolete method (temporary)
    // ============================================
    if (text.Contains("test obsolete"))
    {
#pragma warning disable CS0618 // Obsolete
        var placeholder = activity.ToQuoteReply();
#pragma warning restore CS0618
        await context.Send($"ToQuoteReply() returned: {placeholder}", cancellationToken);
        return;
    }

    // ============================================
    // Help / Default
    // ============================================
    if (text.Contains("help"))
    {
        await context.Send(
            "**Quoted Replies Test Bot**\n\n" +
            "**Commands:**\n" +
            "- `test reply` - Reply() auto-quotes your message\n" +
            "- `test quote` - QuoteReply() quotes a previously sent message\n" +
            "- `test add` - AddQuotedReply() builder with response\n" +
            "- `test multi` - Multi-quote interleaved (quotes two separate messages)\n" +
            "- `test manual` - AddQuotedReply() + AddText() manual control\n" +
            "- `test obsolete` - ToQuoteReply() obsolete method\n\n" +
            "Quote any message to me to see the parsed metadata!", cancellationToken);
        return;
    }

    await context.Send($"You said: '{activity.Text}'\n\nType `help` to see available commands.", cancellationToken);
});

app.Run();
