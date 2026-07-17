// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Handlers.TaskModules;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core;
using TeamsBot;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

teamsApp.UseMiddleware(new WelcomeMessageMiddleware());


// ==================== MESSAGE HANDLERS ====================

// Help handler: matches "help" (case-insensitive)
teamsApp.OnMessage("(?i)^help$", async (context, cancellationToken) =>
{
    await context.SendAsync(
        MessageActivityInput.CreateBuilder()
            .WithText(WelcomeMessageMiddleware.WelcomeMessage, TextFormats.Markdown)
            .Build(), cancellationToken);


    MessageActivityInput helpActivity = MessageActivityInput.CreateBuilder()
        .WithText(WelcomeMessageMiddleware.WelcomeMessage, TextFormats.Markdown)
        .WithSuggestedActions(new SuggestedActions()
        {
            To = [context.Activity.From?.Id!],
            Actions = [
                    new SuggestedAction(ActionTypes.IMBack, "hello"),
                    new SuggestedAction(ActionTypes.IMBack, "feedback"),
                 ]
        })
        .Build();

    await context.SendAsync(helpActivity, cancellationToken);
});

// Pattern-based handler: matches "hello" (case-insensitive)
teamsApp.OnMessage("(?i)hello", async (context, cancellationToken) =>
{
    ArgumentNullException.ThrowIfNull(context.Activity.From);

    await context.TypingAsync(cancellationToken);

    string replyText = $"You sent: `{context.Activity.Text}`. Type `help` to see available commands.";

    MessageActivityInput ta = MessageActivityInput.CreateBuilder()
        .WithText(replyText)
        .AddMention(context.Activity.From)
        .Build();
    await context.SendAsync(ta, cancellationToken);
});

// Extended Markdown handler: matches "extendedMarkdown" (case-insensitive)
teamsApp.OnMessage("(?i)^extendedMarkdown$", async (context, cancellationToken) =>
{
    MessageActivityInput extendedMarkdownMessage = MessageActivityInput.CreateBuilder()
        .WithText("""
# Extended Markdown Demo

## Table
| Feature | Status |
|---------|--------|
| Tables  | Supported |
| Math    | Supported |

## Math
$$E = mc^2$$
""", TextFormats.ExtendedMarkdown)
        .Build();

    await context.SendAsync(extendedMarkdownMessage, cancellationToken);
});

// Markdown handler: matches "markdown" (case-insensitive)
teamsApp.OnMessage("(?i)markdown", async (context, cancellationToken) =>
{
    MessageActivityInput markdownMessage = MessageActivityInput.CreateBuilder()
        .WithText("""
# Markdown Examples

Here are some **markdown** formatting examples:

## Text Formatting
- **Bold text**
- *Italic text*
- ~~Strikethrough~~
- `inline code`

## Lists
1. First item
2. Second item
3. Third item

## Code Block
```csharp
public class Example
{
    public string Name { get; set; }
}
```

## Links
[Visit Microsoft](https://www.microsoft.com)

## Quotes
> This is a blockquote
> It can span multiple lines
""", TextFormats.Markdown)
        .Build();

    await context.SendAsync(markdownMessage, cancellationToken);
});

// Citation handler: matches "citation" (case-insensitive)
teamsApp.OnMessage("(?i)citation", async (context, cancellationToken) =>
{
    MessageActivityInput reply = MessageActivityInput.CreateBuilder()
        .WithText("Here is a response with citations [1] [2].")
        .WithTextFormat(TextFormats.Markdown)
        .AddCitation(1, new CitationAppearance()
        {
            Name = "Teams SDK Documentation",
            Abstract = "The Teams Bot SDK provides a streamlined way to build bots for Microsoft Teams.",
            Url = new Uri("https://github.com/microsoft/teams.net"),
            Icon = CitationIcons.Text
        })
        .AddCitation(2, new CitationAppearance()
        {
            Name = "Bot Framework Overview",
            Abstract = "Build intelligent bots that interact naturally with users on Teams.",
            Keywords = ["bot", "framework"]
        })
        .AddAIGenerated()
        .AddFeedback()
        .Build();

    await context.SendAsync(reply, cancellationToken);
});

// Feedback handler: matches "feedback" (case-insensitive) - sends a feedback card and shows the response via OnAdaptiveCardAction
teamsApp.OnMessage("(?i)^feedback$", async (context, cancellationToken) =>
{
    await context.SendAsync("Please fill out the feedback form below:", cancellationToken);

    TeamsAttachment feedbackCard = TeamsAttachment.CreateBuilder()
            .WithAdaptiveCard(Cards.FeedbackCardObj)
            .Build();
    MessageActivityInput feedbackActivity = MessageActivityInput.CreateBuilder().AddAttachment(feedbackCard).Build();
    await context.SendAsync(feedbackActivity, cancellationToken);
});


// Regex-based handler: matches commands starting with "/"
Regex commandRegex = Regexes.CommandRegex();
teamsApp.OnMessage(commandRegex, async (context, cancellationToken) =>
{
    Match match = commandRegex.Match(context.Activity.Text ?? "");
    if (match.Success)
    {
        string command = match.Groups[1].Value;

        string response = command.ToLower() switch
        {
            "help" => "Available commands: /help, /about, /time",
            "about" => "I'm a Teams bot built with the Microsoft Teams Bot SDK!",
            "time" => $"Current server time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            _ => $"Unknown command: /{command}. Type /help for available commands."
        };

        await context.SendAsync(response, cancellationToken);
    }
});


// ==================== INVOKE HANDLERS ====================

// Adaptive Card action handler: processes feedback form submissions
teamsApp.OnAdaptiveCardAction(async (context, cancellationToken) =>
{
    string? feedbackValue = context.Activity.Value?.Action?.Data?["feedback"]?.ToString();

    MessageActivityInput reply = MessageActivityInput.CreateBuilder()
        .AddAttachment(TeamsAttachment.CreateBuilder()
            .WithAdaptiveCard(Cards.ResponseCard(feedbackValue))
            .Build()
        )
        .Build();

    await context.SendAsync(reply, cancellationToken);

    return AdaptiveCardResponse.CreateMessageResponse("Feedback received!");
});
// ==================== EVENT HANDLERS ====================

teamsApp.OnEvent(async (context, cancellationToken) =>
{
    Console.WriteLine($"[Event] Name: {context.Activity.Name}");
    await context.SendAsync($"Received event: `{context.Activity.Name}`", cancellationToken);
});

webApp.Run();

internal partial class Regexes
{
    [GeneratedRegex(@"^/(\w+)(.*)$")]
    public static partial Regex CommandRegex();
}
