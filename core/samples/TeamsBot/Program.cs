// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Bot.Core;
using Microsoft.Teams.BotApps;
using Microsoft.Teams.BotApps.Schema;
using Microsoft.Teams.BotApps.Schema.Entities;
using TeamsBot;

var builder = TeamsBotApplication.CreateBuilder();
var teamsApp = builder.Build();

teamsApp.OnMessage = async (context, cancellationToken) =>
{
    string replyText = $"You sent: `{context.Activity.Text}` in activity of type `{context.Activity.Type}`.";

    // await context.SendTypingActivityAsync(cancellationToken);

    TeamsActivity reply = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityTypes.Message)
        .WithConversationReference(context.Activity)
        .WithText(replyText)
        .Build();


    reply.AddMention(context.Activity.From!, "ridobotlocal", true);

    await teamsApp.SendActivityAsync(reply, cancellationToken);

    TeamsActivity feedbackCard = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityTypes.Message)
        .WithConversationReference(context.Activity)
        .WithAttachments(
        [
                new TeamsAttachment
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = Cards.FeedbackCardObj
                }
            ]
        )
        .Build();
    await teamsApp.SendActivityAsync(feedbackCard, cancellationToken);
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

    string replyText = $"Invoke activity of type `{context.Activity.Type}` received. Feedback Data {feedbackValue}";
    await context.SendActivityAsync(replyText, cancellationToken);

    return new InvokeResponse(200)
    {
        Body = "Invokes are great !!"
    };
};

teamsApp.Run();
