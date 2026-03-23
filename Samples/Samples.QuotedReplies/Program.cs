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
        await context.Reply("Thanks for your message! This reply auto-quotes it using Reply().", cancellationToken);
        return;
    }

    // ============================================
    // QuoteReply() — quote a previously sent message by ID
    // ============================================
    if (text.Contains("test quote"))
    {
        var sent = await context.Send("The meeting has been moved to 3 PM tomorrow.", cancellationToken);
        await context.QuoteReply(sent.Id, "Just to confirm — does the new time work for everyone?", cancellationToken);
        return;
    }

    // ============================================
    // AddQuotedReply() — builder with response
    // ============================================
    if (text.Contains("test add"))
    {
        var sent = await context.Send("Please review the latest PR before end of day.", cancellationToken);
        var msg = new MessageActivity()
            .AddQuotedReply(sent.Id, "Done! Left my comments on the PR.");
        await context.Send(msg, cancellationToken);
        return;
    }

    // ============================================
    // Multi-quote with mixed responses
    // ============================================
    if (text.Contains("test multi"))
    {
        var sentA = await context.Send("We need to update the API docs before launch.", cancellationToken);
        var sentB = await context.Send("The design mockups are ready for review.", cancellationToken);
        var sentC = await context.Send("CI pipeline is green on main.", cancellationToken);
        var msg = new MessageActivity()
            .AddQuotedReply(sentA.Id, "I can take the docs — will have a draft by Thursday.")
            .AddQuotedReply(sentB.Id, "Looks great, approved!")
            .AddQuotedReply(sentC.Id);
        await context.Send(msg, cancellationToken);
        return;
    }

    // ============================================
    // AddQuotedReply() + AddText() — manual control
    // ============================================
    if (text.Contains("test manual"))
    {
        var sent = await context.Send("Deployment to staging is complete.", cancellationToken);
        var msg = new MessageActivity()
            .AddQuotedReply(sent.Id)
            .AddText(" Verified — all smoke tests passing.");
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
            "- `test multi` - Multi-quote with mixed responses (one bare quote with no response)\n" +
            "- `test manual` - AddQuotedReply() + AddText() manual control\n" +
            "- `test obsolete` - ToQuoteReply() obsolete method\n\n" +
            "Quote any message to me to see the parsed metadata!", cancellationToken);
        return;
    }

    await context.Send($"You said: '{activity.Text}'\n\nType `help` to see available commands.", cancellationToken);
});

app.Run();
