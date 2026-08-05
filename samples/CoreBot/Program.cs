// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Hosting;
using Microsoft.Teams.Core.Schema;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);

// Credentials are read from a custom configuration section instead of the default "AzureAd"
// section, matching the "CustomAuth__*" values in Properties/launchSettings.TEMPLATE.json.
webAppBuilder.Services.AddBotApplication("CustomAuth");
WebApplication webApp = webAppBuilder.Build();

webApp.MapGet("/", () => "CoreBot is running.");
BotApplication botApp = webApp.UseBotApplication();

botApp.OnActivity = async (activity, cancellationToken) =>
{
    string replyText = $"CoreBot running on SDK `{BotApplication.Version}`.";
    ArgumentNullException.ThrowIfNull(activity.Conversation);
    CoreActivityInput replyActivity = CoreActivityInput.CreateBuilder()
        .WithType(ActivityType.Message)
        .WithProperty("text", replyText)
        .Build();

    await botApp.ConversationClient.SendActivityAsync(activity.Conversation.Id!, replyActivity, activity.ServiceUrl!, cancellationToken: cancellationToken);
};

webApp.Run();
