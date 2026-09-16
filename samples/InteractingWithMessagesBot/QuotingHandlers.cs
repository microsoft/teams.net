// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;

internal static class QuotingHandlers
{
    internal static void Register(TeamsBotApplication teamsApp)
    {
        teamsApp.OnMessage("(?i)^quote reply$", async (context, cancellationToken) =>
        {
            await context.ReplyAsync("Thanks for your message! This reply auto-quotes it.", cancellationToken);
        });

        teamsApp.OnMessage("(?i)^quote message$", async (context, cancellationToken) =>
        {
            SendActivityResponse? sent = await context.SendAsync(
                "The meeting has been moved to 3 PM tomorrow.",
                cancellationToken);
            if (sent?.Id != null)
            {
                await context.QuoteAsync(
                    sent.Id,
                    "Just to confirm - does the new time work for everyone?",
                    cancellationToken);
            }
        });

        teamsApp.OnMessage("(?i)^quote add$", async (context, cancellationToken) =>
        {
            SendActivityResponse? sent = await context.SendAsync(
                "Please review the latest PR before end of day.",
                cancellationToken);
            if (sent?.Id != null)
            {
                await context.SendAsync(
                    new MessageActivityInput().AddQuote(sent.Id, "Done! Left my comments on the PR."),
                    cancellationToken);
            }
        });

        teamsApp.OnMessage("(?i)^quote batch$", async (context, cancellationToken) =>
        {
            SendActivityResponse? sentA = await context.SendAsync(
                "We need to update the API docs before launch.",
                cancellationToken);
            SendActivityResponse? sentB = await context.SendAsync(
                "The design mockups are ready for review.",
                cancellationToken);
            SendActivityResponse? sentC = await context.SendAsync(
                "CI pipeline is green on main.",
                cancellationToken);

            if (sentA?.Id != null && sentB?.Id != null && sentC?.Id != null)
            {
                MessageActivityInput message = new MessageActivityInput()
                    .AddQuote(sentA.Id, "I can take the docs - will have a draft by Thursday.")
                    .AddQuote(sentB.Id, "Looks great, approved!")
                    .AddQuote(sentC.Id);
                await context.SendAsync(message, cancellationToken);
            }
        });

        teamsApp.OnMessage("(?i)^quote manual$", async (context, cancellationToken) =>
        {
            SendActivityResponse? sent = await context.SendAsync(
                "Deployment to staging is complete.",
                cancellationToken);
            if (sent?.Id != null)
            {
                await context.SendAsync(
                    new MessageActivityInput()
                        .AddQuote(sent.Id)
                        .AddText(" Verified - all smoke tests passing."),
                    cancellationToken);
            }
        });
    }

    internal static async Task<bool> HandleQuotedMessageAsync(
        Context<MessageActivity> context,
        CancellationToken cancellationToken)
    {
        QuotedReplyData? quote = context.Activity.GetQuotedMessages().FirstOrDefault()?.QuotedReply;
        if (quote == null)
        {
            return false;
        }

        string info = $"Quoted message ID: {quote.MessageId}";
        if (quote.SenderName != null)
        {
            info += $"\nFrom: {quote.SenderName}";
        }
        if (quote.Preview != null)
        {
            info += $"\nPreview: \"{quote.Preview}\"";
        }
        if (quote.IsReplyDeleted == true)
        {
            info += "\n(deleted)";
        }
        if (quote.ValidatedMessageReference == true)
        {
            info += "\n(validated)";
        }

        await context.SendAsync(
            new MessageActivityInput().WithText(
                $"You sent a message with a quoted reply:\n\n{info}",
                TextFormats.Markdown),
            cancellationToken);
        return true;
    }
}
