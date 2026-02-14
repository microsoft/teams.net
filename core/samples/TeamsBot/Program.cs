// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Schema;
using TeamsBot;

var builder = TeamsBotApplication.CreateBuilder(args);
var teamsApp = builder.Build();

// ==================== MESSAGE HANDLERS ====================

// Pattern-based handler: matches "hello" (case-insensitive)
teamsApp.OnMessage("(?i)hello", async (context, cancellationToken) =>
{
    await context.SendActivityAsync("Hi there! 👋 You said hello!", cancellationToken);
});

// Markdown handler: matches "markdown" (case-insensitive)
teamsApp.OnMessage("(?i)markdown", async (context, cancellationToken) =>
{
    var markdownMessage = new MessageActivity("""
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

// Regex-based handler: matches commands starting with "/"
var commandRegex = new Regex(@"^/(\w+)(.*)$", RegexOptions.Compiled);
teamsApp.OnMessage(commandRegex, async (context, cancellationToken) =>
{
    var match = commandRegex.Match(context.Activity.Text ?? "");
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

teamsApp.OnMessageReaction( async (context, cancellationToken) =>
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
    var valueNode = context.Activity.Value;
    string? feedbackValue = valueNode?["action"]?["data"]?["feedback"]?.GetValue<string>();

    var reply = TeamsActivity.CreateBuilder()
        .WithAttachment(TeamsAttachment.CreateBuilder()
            .WithAdaptiveCard(Cards.ResponseCard(feedbackValue))
            .Build()
        )
        .Build();

    await context.SendActivityAsync(reply, cancellationToken);

    return AdaptiveCardResponse.CreateMessageResponse("Invokes are great!!");
});

// ==================== CONVERSATION UPDATE HANDLERS ====================

teamsApp.OnMembersAdded(async (context, cancellationToken) =>
{
    Console.WriteLine($"[MembersAdded] {context.Activity.MembersAdded?.Count ?? 0} member(s) added");

    var memberNames = string.Join(", ", context.Activity.MembersAdded?.Select(m => m.Name ?? m.Id) ?? []);
    await context.SendActivityAsync($"Welcome! Members added: {memberNames}", cancellationToken);
});

teamsApp.OnMembersRemoved(async (context, cancellationToken) =>
{
    Console.WriteLine($"[MembersRemoved] {context.Activity.MembersRemoved?.Count ?? 0} member(s) removed");

    var memberNames = string.Join(", ", context.Activity.MembersRemoved?.Select(m => m.Name ?? m.Id) ?? []);
    await context.SendActivityAsync($"Goodbye! Members removed: {memberNames}", cancellationToken);
});

// ==================== INSTALL UPDATE HANDLERS ====================

teamsApp.OnInstallUpdate(async (context, cancellationToken) =>
{
    var action = context.Activity.Action ?? "unknown";
    Console.WriteLine($"[InstallUpdate] Installation action: {action}");
    await context.SendActivityAsync($"Installation update: {action}", cancellationToken);
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


teamsApp.Run();
