// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.BotApps;
using Microsoft.Teams.BotApps.Schema;
using Microsoft.Teams.BotApps.Schema.Entities;

var builder = TeamsBotApplication.CreateBuilder();
var teamsApp = builder.Build();

teamsApp.OnMessage = async (context, cancellationToken) =>
{
    string replyText = $"You sent: `{context.Activity.Text}` in activity of type `{context.Activity.Type}`.";

    await context.SendTypingActivityAsync(cancellationToken);

    TeamsActivity reply = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityTypes.Message)
        .WithConversationReference(context.Activity)
        .WithText(replyText)
        .Build();

    reply.AddMention(context.Activity.From!, "ridobotlocal", true);

    await context.TeamsBotApplication.SendActivityAsync(reply, cancellationToken);
};

teamsApp.Run();
