// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core.Schema;
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
    await context.SendActivityAsync(
        new MessageActivity(WelcomeMessageMiddleware.WelcomeMessage)
        {
            TextFormat = TextFormats.Markdown
        }, cancellationToken);

    var helpMessage = """
**Teams Bot Demo**

**Messages**
- `hello` - Greeting
- `markdown` - Markdown formatting demo
- `citation` - AI citations with feedback
- `targeted` - Targeted message lifecycle (send, update, delete)
- `react` - Bot reactions (add, remove)
- `card` - Send an Adaptive Card with a feedback form
- `feedback` - Feedback form with Adaptive Card action round-trip
- `task` - Open a task module dialog

**Commands**
- `/help` - Available slash commands
- `/about` - About this bot
- `/time` - Current server time

**Lifecycle** *(automatic)*
- Message edits, deletes, and reactions are detected
- Member join/leave and install/uninstall events are handled
""";

    var helpActivity = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityType.Message)
        .WithText(helpMessage, TextFormats.Markdown)
        .WithSuggestedActions(new SuggestedActions()
         {
             To = [context.Activity.From?.Id!],
             Actions = [
                    new SuggestedAction(ActionType.IMBack, "hello") { Value = "hello" },
                    new SuggestedAction(ActionType.IMBack, "feedback") { Value = "feedback" },
                 ]
         })
        .Build();

    await context.SendActivityAsync(helpActivity, cancellationToken);
});

// Pattern-based handler: matches "hello" (case-insensitive)
teamsApp.OnMessage("(?i)hello", async (context, cancellationToken) =>
{
    ArgumentNullException.ThrowIfNull(context.Activity.From);

    await context.SendTypingActivityAsync(cancellationToken);

    string replyText = $"You sent: `{context.Activity.Text}`. Type `help` to see available commands.";

    TeamsActivity ta = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityType.Message)
        .WithText(replyText)
        .AddMention(context.Activity.From)
        .Build();
    await context.SendActivityAsync(ta, cancellationToken);
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
        Url = new Uri("https://github.com/microsoft/teams.net"),
        Icon = CitationIcon.Text
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

// Targeted message handler: matches "targeted" (case-insensitive)
// Demonstrates send, update, and delete of a targeted message using Recipient.IsTargeted
teamsApp.OnMessage("(?i)targeted", async (context, cancellationToken) =>
{
    ArgumentNullException.ThrowIfNull(context.Activity.From);
    ArgumentNullException.ThrowIfNull(context.Activity.Conversation);
    ArgumentNullException.ThrowIfNull(context.Activity.ServiceUrl);

    // Send a targeted message visible only to the sender
    TeamsActivity targeted = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityType.Message)
        .WithText("This is a targeted message only you can see!")
        .WithRecipient(context.Activity.From, isTargeted: true)
        .Build();

    var sendResponse = await context.SendActivityAsync(targeted, cancellationToken);

    await Task.Delay(2000, cancellationToken);

    // Update the targeted message (must use UpdateTargetedAsync to avoid setting Recipient on the update payload)
    TeamsActivity updated = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityType.Message)
        .WithText("This targeted message was updated!")
        .WithServiceUrl(context.Activity.ServiceUrl)
        .Build();

    await context.TeamsBotApplication.Api.Conversations.Activities.UpdateTargetedAsync(
        context.Activity.Conversation.Id!,
        sendResponse!.Id!,
        updated,
        cancellationToken: cancellationToken);

    await Task.Delay(2000, cancellationToken);

    // Delete the targeted message
    await context.TeamsBotApplication.Api.Conversations.Activities.DeleteTargetedAsync(
        context.Activity.Conversation.Id!,
        sendResponse.Id!,
        context.Activity.ServiceUrl,
        cancellationToken: cancellationToken);
});

// Reactions handler: matches "react" (case-insensitive) - adds and removes bot reactions on a message
teamsApp.OnMessage("(?i)^react$", async (context, cancellationToken) =>
{
    ArgumentNullException.ThrowIfNull(context.Activity.Conversation);
    ArgumentNullException.ThrowIfNull(context.Activity.ServiceUrl);

    var tmMsgToReact = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityType.Message)
        .WithText("I'm going to add and remove reactions to this message.")
        .WithRecipient(context.Activity.From, false)
        .WithServiceUrl(context.Activity.ServiceUrl)
        .Build();

    var response = await context.SendActivityAsync(tmMsgToReact, cancellationToken);

    await Task.Delay(2000, cancellationToken);

    // Add a waving hand reaction
    await context.TeamsBotApplication.Api.Conversations.Reactions.AddAsync(
        context.Activity,
        response!.Id!,
        "1f44b_wavinghand-tone4",
        cancellationToken: cancellationToken);

    await Task.Delay(2000, cancellationToken);

    // Add a beaming face reaction
    await context.TeamsBotApplication.Api.Conversations.Reactions.AddAsync(
        context.Activity,
        response.Id!,
        "1f601_beamingfacewithsmilingeyes",
        cancellationToken: cancellationToken);

    await Task.Delay(2000, cancellationToken);

    // Remove the beaming face reaction
    await context.TeamsBotApplication.Api.Conversations.Reactions.DeleteAsync(
        context.Activity,
        response.Id!,
        "1f601_beamingfacewithsmilingeyes",
        cancellationToken: cancellationToken);
});

// Card handler: matches "card" (case-insensitive) - sends an adaptive card with a feedback form
teamsApp.OnMessage("(?i)^card$", async (context, cancellationToken) =>
{
    TeamsAttachment feedbackCard = TeamsAttachment.CreateBuilder()
            .WithAdaptiveCard(JsonElement.Parse(Cards.TimeOffRequestCardJson))
            .Build();
    MessageActivity feedbackActivity = new([feedbackCard]);
    await context.SendActivityAsync(feedbackActivity, cancellationToken);
});

// Feedback handler: matches "feedback" (case-insensitive) - sends a feedback card and shows the response via OnAdaptiveCardAction
teamsApp.OnMessage("(?i)^feedback$", async (context, cancellationToken) =>
{
    await context.SendActivityAsync("Please fill out the feedback form below:", cancellationToken);

    TeamsAttachment feedbackCard = TeamsAttachment.CreateBuilder()
            .WithAdaptiveCard(Cards.FeedbackCardObj)
            .Build();
    MessageActivity feedbackActivity = new([feedbackCard]);
    await context.SendActivityAsync(feedbackActivity, cancellationToken);
});

// Task handler: matches "task" (case-insensitive) - sends a card that opens a task module
teamsApp.OnMessage("(?i)^task$", async (context, cancellationToken) =>
{
    TeamsAttachment taskCard = TeamsAttachment.CreateBuilder()
            .WithAdaptiveCard(Cards.TaskModuleLauncherCard)
            .Build();
    MessageActivity taskActivity = new([taskCard]);
    await context.SendActivityAsync(taskActivity, cancellationToken);
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

        await context.SendActivityAsync(response, cancellationToken);
    }
});

// Catch-all message handler: echoes the message back with a mention
teamsApp.OnMessage(async (context, cancellationToken) =>
{
    await context.SendTypingActivityAsync(cancellationToken);

    ArgumentNullException.ThrowIfNull(context.Activity.From);

    string replyText = $"You sent: `{context.Activity.Text}`. Type `help` to see available commands.";

    TeamsActivity ta = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityType.Message)
        .WithText(replyText)
        .AddMention(context.Activity.From)
        .Build();

    await context.SendActivityAsync(ta, cancellationToken);
});

// ==================== MESSAGE LIFECYCLE ====================

teamsApp.OnMessageUpdate(async (context, cancellationToken) =>
{
    string updatedText = context.Activity.Text ?? "<no text>";
    MessageActivity reply = new($"I saw that you updated your message to: `{updatedText}`");
    await context.SendActivityAsync(reply, cancellationToken);
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

// ==================== INVOKE HANDLERS ====================

// Adaptive Card action handler: processes feedback form submissions
teamsApp.OnAdaptiveCardAction(async (context, cancellationToken) =>
{
    string? feedbackValue = context.Activity.Value?.Action?.Data?["feedback"]?.ToString();

    TeamsActivity reply = TeamsActivity.CreateBuilder()
        .WithAttachment(TeamsAttachment.CreateBuilder()
            .WithAdaptiveCard(Cards.ResponseCard(feedbackValue))
            .Build()
        )
        .Build();

    await context.SendActivityAsync(reply, cancellationToken);

    return AdaptiveCardResponse.CreateMessageResponse("Feedback received!");
});

// Task module fetch: returns an Adaptive Card dialog
teamsApp.OnTaskFetch(async (context, cancellationToken) =>
{
    await Task.CompletedTask;

    return TaskModuleResponse.CreateBuilder()
        .WithType(TaskModuleResponseType.Continue)
        .WithTitle("Task Module Demo")
        .WithHeight(TaskModuleSize.Medium)
        .WithWidth(TaskModuleSize.Medium)
        .WithCard(TeamsAttachment.CreateBuilder()
            .WithAdaptiveCard(Cards.TaskModuleFormCard)
            .Build())
        .Build();
});

// Task module submit: processes the task module form submission
teamsApp.OnTaskSubmit(async (context, cancellationToken) =>
{
    JsonNode? data = context.Activity.Value?.Data is System.Text.Json.JsonElement je
        ? JsonNode.Parse(je.GetRawText())
        : null;

    string? name = data?["userName"]?.ToString();
    string? comment = data?["userComment"]?.ToString();

    TeamsActivity reply = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityType.Message)
        .WithText($"**Task module submitted!**\n- Name: {name ?? "(empty)"}\n- Comment: {comment ?? "(empty)"}")
        .Build();

    await context.SendActivityAsync(reply, cancellationToken);

    return TaskModuleResponse.CreateBuilder()
        .WithType(TaskModuleResponseType.Message)
        .WithMessage($"Thanks {name ?? "there"}! Your response was recorded.")
        .Build();
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

// TODO: This do not trigger from the TimeOffCard submission, need to investigate if it's an issue with the card or the handler
teamsApp.OnMessageSubmitAction(async (context, cancellationToken) =>
{
    var actionData = JsonSerializer.Serialize(context.Activity.Value);
    await context.SendActivityAsync($"Received submit action with data: {actionData}", cancellationToken);
    return new InvokeResponse(200, "Submit Action Received");
});

webApp.Run();

partial class Regexes
{
    [GeneratedRegex(@"^/(\w+)(.*)$")]
    public static partial Regex CommandRegex();
}
