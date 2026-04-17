// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FluentCards;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

webApp.MapGet("/", () => "Diag is running.");
var botApp = webApp.UseTeamsBotApplication();

//botApp.OnActivity = async (activity, cancellationToken) =>
//{
//    string replyText = $"DiagBot running on SDK `{BotApplication.Version}`.";

//    CoreActivity replyActivity = CoreActivity.CreateBuilder()
//        .WithType(ActivityType.Message)
//        .WithConversationReference(activity)
//        .WithProperty("text", replyText)
//        .Build();

//    await botApp.SendActivityAsync(replyActivity, cancellationToken);
//};

botApp.OnMessage(async (ctx, ct) =>
{
    var tcid = ctx.Activity.ChannelData?.TeamsChannelId;
    var convType = ctx.Activity.Conversation?.ConversationType;
    var isGroup = ctx.Activity.Conversation?.IsGroup;

    var cardBuilder = AdaptiveCardBuilder.Create()
        .AddTextBlock(tb => tb
            .WithText("Conversation Diagnostics")
            .WithSize(TextSize.Large)
            .WithWeight(TextWeight.Bolder))
        .AddFactSet(fs => fs
            .AddFact("isGroup", isGroup.ToString()!)
            .AddFact("convType", convType!));

    

    if (convType != ConversationType.Personal)
    {
        var members = await ctx.Api.Conversations.Members.GetAsync(ctx.Activity.Conversation?.Id!, ct);
        foreach (var member in members)
        {
            cardBuilder.AddFactSet(fs => fs
                .AddFact(member.Name!, member.Id?.Substring(0,8)! + "..."));
        }
    }

    var msg = TeamsActivity.CreateBuilder()
            .WithAdaptiveCardAttachment(cardBuilder.Build().ToJsonElement())
            .Build();
    await ctx.SendActivityAsync(msg, ct);


});

webApp.Run();
