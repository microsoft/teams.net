// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core.Hosting;
using Microsoft.Teams.BotApps;
using Microsoft.Teams.BotApps.Schema;
using Microsoft.Teams.BotApps.Schema.Entities;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddBotApplication<TeamsBotApplication>();
WebApplication webApp = webAppBuilder.Build();
TeamsBotApplication teamsApp = webApp.UseBotApplication<TeamsBotApplication>();

webApp.MapGet("/", () => "CoreBot is running.");

teamsApp.OnMessage = async (context, cancellationToken) =>
{
    string replyText = $"CoreBot running on Teams SDK {TeamsBotApplication.Version}.";
    replyText += $"<br /> You sent: `{context.Activity.Text}` in activity of type `{context.Activity.Type}`.";

    TeamsActivity reply = TeamsActivity.CreateBuilder()
        .WithType(TeamsActivityTypes.Message)
        .WithConversationReference(context.Activity)
        .WithText(replyText)
        .Build();

    reply.AddMention(context.Activity.From!, "ridobotlocal", true);

    await context.TeamsBotApplication.SendActivityAsync(reply, cancellationToken);
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

webApp.Run();
