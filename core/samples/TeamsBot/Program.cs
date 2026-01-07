// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.BotApps;
using Microsoft.Teams.BotApps.Schema;
using Microsoft.Teams.BotApps.Schema.Entities;

var builder = TeamsBotApplication.CreateBuilder();
var teamsApp = builder.Build();

teamsApp.OnMessage = async (messageArgs, context, cancellationToken) =>
{
    string replyText = $"You sent: `{messageArgs.Text}` in activity of type `{context.Activity.Type}`.";

    // await context.SendTypingActivityAsync(cancellationToken);

    TeamsActivity reply = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityType.Message)
        .WithConversationReference(context.Activity)
        .WithText(replyText)
        .Build();


    reply.AddMention(context.Activity.From!, "ridobotlocal", true);

    await teamsApp.SendActivityAsync(reply, cancellationToken);
    await context.SendActivityAsync("Mention sent!", cancellationToken);
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

teamsApp.Run();
