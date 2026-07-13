// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

// Inbound quoted replies — fires on every message, echoes metadata when a quote is present.
teamsApp.OnMessage(async (context, cancellationToken) =>
{
    QuotedReplyData? quote = context.Activity.GetQuotedMessages().FirstOrDefault()?.QuotedReply;
    if (quote == null) return;

    string info = $"Quoted message ID: {quote.MessageId}";
    if (quote.SenderName != null) info += $"\nFrom: {quote.SenderName}";
    if (quote.Preview != null) info += $"\nPreview: \"{quote.Preview}\"";
    if (quote.IsReplyDeleted == true) info += "\n(deleted)";
    if (quote.ValidatedMessageReference == true) info += "\n(validated)";

    await context.SendActivityAsync(
        MessageActivity.CreateBuilder().WithText($"You sent a message with a quoted reply:\n\n{info}", TextFormats.Markdown).Build(),
        cancellationToken);
});

// ReplyAsync() — auto-quotes the inbound message
teamsApp.OnMessage("(?i)^test reply$", async (context, cancellationToken) =>
{
    await context.ReplyAsync("Thanks for your message! This reply auto-quotes it.", cancellationToken);
});

// QuoteAsync() — quote a previously sent message by ID
teamsApp.OnMessage("(?i)^test quote$", async (context, cancellationToken) =>
{
    SendActivityResponse? sent = await context.SendActivityAsync("The meeting has been moved to 3 PM tomorrow.", cancellationToken);
    if (sent?.Id != null)
    {
        await context.QuoteAsync(sent.Id, "Just to confirm — does the new time work for everyone?", cancellationToken);
    }
});

// AddQuote() builder method — fluent API
teamsApp.OnMessage("(?i)^test add$", async (context, cancellationToken) =>
{
    SendActivityResponse? sent = await context.SendActivityAsync("Please review the latest PR before end of day.", cancellationToken);
    if (sent?.Id != null)
    {
        MessageActivity msg = MessageActivity.CreateBuilder()
            .AddQuote(sent.Id, "Done! Left my comments on the PR.")
            .Build();
        await context.SendActivityAsync(msg, cancellationToken);
    }
});

// Multi-quote with mixed responses
teamsApp.OnMessage("(?i)^test multi$", async (context, cancellationToken) =>
{
    SendActivityResponse? sentA = await context.SendActivityAsync("We need to update the API docs before launch.", cancellationToken);
    SendActivityResponse? sentB = await context.SendActivityAsync("The design mockups are ready for review.", cancellationToken);
    SendActivityResponse? sentC = await context.SendActivityAsync("CI pipeline is green on main.", cancellationToken);

    if (sentA?.Id != null && sentB?.Id != null && sentC?.Id != null)
    {
        MessageActivity msg = MessageActivity.CreateBuilder()
            .AddQuote(sentA.Id, "I can take the docs — will have a draft by Thursday.")
            .AddQuote(sentB.Id, "Looks great, approved!")
            .AddQuote(sentC.Id)
            .Build();
        await context.SendActivityAsync(msg, cancellationToken);
    }
});

// Help
teamsApp.OnMessage("(?i)^help$", async (context, cancellationToken) =>
{
    await context.SendActivityAsync(
        MessageActivity.CreateBuilder()
            .WithText(
            "**Quoting Test Bot**\n\n" +
            "**Commands:**\n" +
            "- `test reply` - Reply() auto-quotes your message\n" +
            "- `test quote` - Quote() quotes a previously sent message\n" +
            "- `test add` - AddQuote() extension with response\n" +
            "- `test multi` - Multi-quote with mixed responses\n" +
            "Quote any message to me to see the parsed metadata!", TextFormats.Markdown)
            .Build(),
        cancellationToken);
});

webApp.Run();
