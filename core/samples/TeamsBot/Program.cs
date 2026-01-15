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

    reply.AddMention(context.Activity.From!);

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
    string reactionsAdded = string.Join(", ", args.ReactionsAdded?.Select(r => r.Type) ?? []);
    string reactionsRemoved = string.Join(", ", args.ReactionsRemoved?.Select(r => r.Type) ?? []);

    var reply = TeamsActivity.CreateBuilder()
        .WithAttachment(TeamsAttachment.CreateBuilder()
            .WithAdaptiveCard(Cards.ReactionsCard(reactionsAdded, reactionsRemoved))
            .Build()
        )
        .Build();

    await context.SendActivityAsync(reply, cancellationToken);
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

    return new CoreInvokeResponse(200)
    {
        Type = "application/vnd.microsoft.activity.message",
        Body = "Invokes are great !!"
    };
};

//teamsApp.OnActivity = async (activity, ct) =>
//{
//    var reply = CoreActivity.CreateBuilder()
//        .WithConversationReference(activity)
//        .WithProperty("text", "yo")
//        .Build();
//    await teamsApp.SendActivityAsync(reply, ct);
//};


teamsApp.Run();
