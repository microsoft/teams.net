

using Microsoft.Bot.Core;
using Microsoft.Bot.Core.Hosting;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddBotApplication<BotApplication>();
WebApplication webApp = webAppBuilder.Build();
var botApp = webApp.UseBotApplication<BotApplication>();


botApp.OnActivity = async (activity, cancellationToken) =>
{
    string replyText = $"CoreBot running on SDK {BotApplication.Version}.";
    replyText += $"\r\nYou sent: `{activity.Text}` in activity of type `{activity.Type}`.";
    replyText += $"\r\n to Conversation ID: `{activity.Conversation.Id}` type: `{activity.Conversation.Properties["conversationType"]}`";
    var replyActivity = activity.CreateReplyMessageActivity(replyText);
    await botApp.SendActivityAsync(replyActivity, cancellationToken);
};

webApp.Run();
