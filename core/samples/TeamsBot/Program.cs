// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.BotApps;
using Microsoft.Teams.BotApps.Handlers;
using Microsoft.Teams.BotApps.Schema;
using Microsoft.Teams.BotApps.Schema.Entities;
using TeamsBot;

var builder = TeamsBotApplication.CreateBuilder();
var teamsApp = builder.Build();

teamsApp.OnMessage = async (messageArgs, context, cancellationToken) =>
{
    await context.SendTypingActivityAsync(cancellationToken);

    string replyText = $"You sent: `{messageArgs.Text}` in activity of type `{context.Activity.Type}`.";

    TeamsActivity reply = TeamsActivity.CreateBuilder()
        .WithText(replyText)
        .Build();

    reply.AddMention(context.Activity.From!, "ridobotlocal", true);

    await context.SendActivityAsync(reply, cancellationToken);

    TeamsActivity feedbackCard = TeamsActivity.CreateBuilder()
        .WithAttachment(TeamsAttachment.CreateBuilder()
            .WithAdaptiveCard(Cards.FeedbackCardObj)
            .Build())
        .Build();
    await context.SendActivityAsync(feedbackCard, cancellationToken);
};

teamsApp.OnMessageReaction = async (args, context, cancellationToken) =>
{
    string replyText = $"Message reaction activity of type `{context.Activity.Type}` received.";
    replyText += args.ReactionsAdded != null
        ? $"<br /> Reactions Added: {string.Join(", ", args.ReactionsAdded.Select(r => r.Type))}."
        : string.Empty;
    replyText += args.ReactionsRemoved != null
   ? $"<br /> Reactions Removed: {string.Join(", ", args.ReactionsRemoved.Select(r => r.Type))}."
       : string.Empty;

    await context.SendActivityAsync(replyText, cancellationToken);
};

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

    return new CoreInvokeResponse(209)
    {
        Type = "application/vnd.microsoft.activity.message",
        Body = "Invokes are great !!"
    };
};

teamsApp.Run();
