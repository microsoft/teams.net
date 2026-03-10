// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Schema;
using TeamsBot;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

// ==================== MESSAGE HANDLERS ====================

// Pattern-based handler: matches "hello" (case-insensitive)
teamsApp.OnMessage("(?i)hello", async (context, cancellationToken) =>
{
    await context.SendActivityAsync("Hi there! 👋 You said hello!", cancellationToken);
});

// Markdown handler: matches "markdown" (case-insensitive)
teamsApp.OnMessage("(?i)markdown", async (context, cancellationToken) =>
{
    MessageActivity markdownMessage = new("""
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
""")
    {
        TextFormat = TextFormats.Markdown
    };

    await context.SendActivityAsync(markdownMessage, cancellationToken);
});

// Citation handler: matches "citation" (case-insensitive)
teamsApp.OnMessage("(?i)citation", async (context, cancellationToken) =>
{
    MessageActivity reply = new("Here is a response with citations [1] [2].")
    {
        TextFormat = TextFormats.Markdown
    };

    reply.AddCitation(1, new CitationAppearance()
    {
        Name = "Teams SDK Documentation",
        Abstract = "The Teams Bot SDK provides a streamlined way to build bots for Microsoft Teams.",
        Url = new Uri("https://github.com/nicoco007/microsoft/teams.net"),
        Icon = CitationIcon.Text,
        EncodingFormat = EncodingFormats.AdaptiveCard
    });

    reply.AddCitation(2, new CitationAppearance()
    {
        Name = "Bot Framework Overview",
        Abstract = "Build intelligent bots that interact naturally with users on Teams.",
        Keywords = ["bot", "framework"]
    });

    reply.AddAIGenerated();
    reply.AddFeedback();

    await context.SendActivityAsync(reply, cancellationToken);
});

// Regex-based handler: matches commands starting with "/"
Regex commandRegex = Regexes.CommandRegex();
teamsApp.OnMessage(commandRegex, async (context, cancellationToken) =>
{
    Match match = commandRegex.Match(context.Activity.Text ?? "");
    if (match.Success)
    {
        string command = match.Groups[1].Value;
        string args = match.Groups[2].Value.Trim();

        string response = command.ToLower() switch
        {
            "help" => "Available commands: /help, /about, /time",
            "about" => "I'm a Teams bot built with the Microsoft Teams Bot SDK!",
            "time" => $"Current server time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            _ => $"Unknown command: /{command}. Type /help for available commands."
        };

        await context.SendActivityAsync(response, cancellationToken);
    }
});

teamsApp.OnMessageUpdate(async (context, cancellationToken) =>
{
    string updatedText = context.Activity.Text ?? "<no text>";
    MessageActivity reply = new($"I saw that you updated your message to: `{updatedText}`");
    await context.SendActivityAsync(reply, cancellationToken);
});

teamsApp.OnMessage(async (context, cancellationToken) =>
{
    await context.SendTypingActivityAsync(cancellationToken);

    string replyText = $"You sent: `{context.Activity.Text}` in activity of type `{context.Activity.Type}`.";

    MessageActivity reply = new(replyText);
    reply.AddMention(context.Activity.From!);

    await context.SendActivityAsync(reply, cancellationToken);

    TeamsAttachment feedbackCard = TeamsAttachment.CreateBuilder()
            .WithAdaptiveCard(Cards.FeedbackCardObj)
            .Build();
    MessageActivity feedbackActivity = new([feedbackCard]);
    await context.SendActivityAsync(feedbackActivity, cancellationToken);
});

teamsApp.OnMessageReaction(async (context, cancellationToken) =>
{
    string reactionsAdded = string.Join(", ", context.Activity.ReactionsAdded?.Select(r => r.Type) ?? []);
    string reactionsRemoved = string.Join(", ", context.Activity.ReactionsRemoved?.Select(r => r.Type) ?? []);

    TeamsAttachment reactionsCard = TeamsAttachment.CreateBuilder()
            .WithAdaptiveCard(Cards.ReactionsCard(reactionsAdded, reactionsRemoved))
            .Build();
    MessageActivity reply = new([reactionsCard]);

    await context.SendActivityAsync(reply, cancellationToken);
});

teamsApp.OnMessageDelete(async (context, cancellationToken) =>
{

    await context.SendActivityAsync("I saw that message you deleted", cancellationToken);
});

// ==================== INVOKE ====================

teamsApp.OnInvoke(async (context, cancellationToken) =>
{
    JsonNode? valueNode = context.Activity.Value;
    string? feedbackValue = valueNode?["action"]?["data"]?["feedback"]?.GetValue<string>();

    TeamsActivity reply = TeamsActivity.CreateBuilder()
        .WithAttachment(TeamsAttachment.CreateBuilder()
            .WithAdaptiveCard(Cards.ResponseCard(feedbackValue))
            .Build()
        )
        .Build();

    await context.SendActivityAsync(reply, cancellationToken);

    return AdaptiveCardResponse.CreateMessageResponse("Invokes are great!!");
});

// ==================== EVENT HANDLERS ====================

teamsApp.OnEvent(async (context, cancellationToken) =>
{
    Console.WriteLine($"[Event] Name: {context.Activity.Name}");
    await context.SendActivityAsync($"Received event: `{context.Activity.Name}`", cancellationToken);
});

// ==================== CONVERSATION UPDATE HANDLERS ====================

teamsApp.OnMembersAdded(async (context, cancellationToken) =>
{
    Console.WriteLine($"[MembersAdded] {context.Activity.MembersAdded?.Count ?? 0} member(s) added");

    string memberNames = string.Join(", ", context.Activity.MembersAdded?.Select(m => m.Name ?? m.Id) ?? []);
    await context.SendActivityAsync($"Welcome! Members added: {memberNames}", cancellationToken);
});

teamsApp.OnMembersRemoved(async (context, cancellationToken) =>
{
    Console.WriteLine($"[MembersRemoved] {context.Activity.MembersRemoved?.Count ?? 0} member(s) removed");

    string memberNames = string.Join(", ", context.Activity.MembersRemoved?.Select(m => m.Name ?? m.Id) ?? []);
    await context.SendActivityAsync($"Goodbye! Members removed: {memberNames}", cancellationToken);
});

// ==================== INSTALL UPDATE HANDLERS ====================

teamsApp.OnInstallUpdate(async (context, cancellationToken) =>
{
    string action = context.Activity.Action ?? "unknown";
    Console.WriteLine($"[InstallUpdate] Installation action: {action}");

    if (context.Activity.Action != InstallUpdateActions.Remove)
    {
        await context.SendActivityAsync($"Installation update: {action}", cancellationToken);
    }
});

teamsApp.OnInstall(async (context, cancellationToken) =>
{
    Console.WriteLine($"[InstallAdd] Bot was installed");
    await context.SendActivityAsync("Thanks for installing me! I'm ready to help.", cancellationToken);
});

teamsApp.OnUnInstall((context, cancellationToken) =>
{
    Console.WriteLine($"[InstallRemove] Bot was uninstalled");
    return Task.CompletedTask;
});

webApp.Run();

partial class Regexes
{
    [GeneratedRegex(@"^/(\w+)(.*)$")]
    public static partial Regex CommandRegex();
}
