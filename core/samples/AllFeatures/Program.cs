// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.Entities;

var builder = TeamsBotApplication.CreateBuilder();
var teamsApp = builder.Build();

teamsApp.OnMessage = async (messageArgs, context, cancellationToken) =>
{
    string replyText = $"You sent: `{messageArgs.Text}` in activity of type `{context.Activity.Type}`.";

    await context.SendTypingActivityAsync(cancellationToken);

    TeamsActivity reply = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityType.Message)
        .WithConversationReference(context.Activity)
        .WithText(replyText)
        .Build();

    reply.AddMention(context.Activity.From!, "ridobotlocal", true);

    await context.TeamsBotApplication.SendActivityAsync(reply, cancellationToken);
};

teamsApp.Run();
