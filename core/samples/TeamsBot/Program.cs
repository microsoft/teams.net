// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.Entities;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;
using TeamsBot;

var builder = TeamsBotApplication.CreateBuilder();
var teamsApp = builder.Build();


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

/*
teamsApp.OnInvoke = async (context, cancellationToken) =>
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

    return new CoreInvokeResponse(200)
    {
        Type = "application/vnd.microsoft.activity.message",
        Body = "Invokes are great !!"
    };
};
*/

teamsApp.Run();
