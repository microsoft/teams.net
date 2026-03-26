// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.Entities;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

teamsApp.OnMessage(async (context, cancellationToken) =>
{
    var activity = context.Activity;
    var text = activity.Text?.ToLowerInvariant()?.Trim() ?? "";

    // Read inbound quoted replies
    var quotes = activity.GetQuotedMessages().ToList();
    if (quotes.Count > 0)
    {
        var quote = quotes[0].QuotedReply;
        var info = $"Quoted message ID: {quote?.MessageId}";
        if (quote?.SenderName != null) info += $"\nFrom: {quote.SenderName}";
        if (quote?.Preview != null) info += $"\nPreview: \"{quote.Preview}\"";
        if (quote?.IsReplyDeleted == true) info += "\n(deleted)";
        if (quote?.ValidatedMessageReference == true) info += "\n(validated)";

        await context.SendActivityAsync(
            new MessageActivity($"You sent a message with a quoted reply:\n\n{info}") { TextFormat = TextFormats.Markdown },
            cancellationToken);
        return;
    }

    // ReplyAsync() — auto-quotes the inbound message
    if (text.Contains("test reply"))
    {
        await context.ReplyAsync("Thanks for your message! This reply auto-quotes it.", cancellationToken);
        return;
    }

    // QuoteAsync() — quote a previously sent message by ID
    if (text.Contains("test quote"))
    {
        var sent = await context.SendActivityAsync("The meeting has been moved to 3 PM tomorrow.", cancellationToken);
        if (sent?.Id != null)
        {
            await context.QuoteAsync(sent.Id, "Just to confirm — does the new time work for everyone?", cancellationToken);
        }
        return;
    }

    // AddQuote() extension — builder with response
    if (text.Contains("test add"))
    {
        var sent = await context.SendActivityAsync("Please review the latest PR before end of day.", cancellationToken);
        if (sent?.Id != null)
        {
            MessageActivity msg = new();
            msg.AddQuote(sent.Id, "Done! Left my comments on the PR.");
            await context.SendActivityAsync(msg, cancellationToken);
        }
        return;
    }

    // Multi-quote with mixed responses
    if (text.Contains("test multi"))
    {
        var sentA = await context.SendActivityAsync("We need to update the API docs before launch.", cancellationToken);
        var sentB = await context.SendActivityAsync("The design mockups are ready for review.", cancellationToken);
        var sentC = await context.SendActivityAsync("CI pipeline is green on main.", cancellationToken);

        if (sentA?.Id != null && sentB?.Id != null && sentC?.Id != null)
        {
            MessageActivity msg = new();
            msg.AddQuote(sentA.Id, "I can take the docs — will have a draft by Thursday.");
            msg.AddQuote(sentB.Id, "Looks great, approved!");
            msg.AddQuote(sentC.Id);
            await context.SendActivityAsync(msg, cancellationToken);
        }
        return;
    }

    // Builder pattern — WithQuote on TeamsActivityBuilder
    if (text.Contains("test builder"))
    {
        var sent = await context.SendActivityAsync("Deployment to staging is complete.", cancellationToken);
        if (sent?.Id != null)
        {
            TeamsActivity reply = TeamsActivity.CreateBuilder()
                .WithType(TeamsActivityType.Message)
                .WithQuote(sent.Id, "Verified — all smoke tests passing.")
                .Build();
            await context.SendActivityAsync(reply, cancellationToken);
        }
        return;
    }

    // Help / Default
    await context.SendActivityAsync(
        new MessageActivity(
            "**Quoted Replies Test Bot**\n\n" +
            "**Commands:**\n" +
            "- `test reply` - ReplyAsync() auto-quotes your message\n" +
            "- `test quote` - QuoteAsync() quotes a previously sent message\n" +
            "- `test add` - AddQuote() extension with response\n" +
            "- `test multi` - Multi-quote with mixed responses\n" +
            "- `test builder` - WithQuote() on TeamsActivityBuilder\n\n" +
            "Quote any message to me to see the parsed metadata!")
        { TextFormat = TextFormats.Markdown },
        cancellationToken);
});

webApp.Run();
