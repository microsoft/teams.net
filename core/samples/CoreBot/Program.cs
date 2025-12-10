

using Microsoft.Bot.Core;
using Microsoft.Bot.Core.Hosting;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddBotApplication<BotApplication>();
WebApplication webApp = webAppBuilder.Build();
var botApp = webApp.UseBotApplication<BotApplication>();


botApp.OnActivity = async (activity, cancellationToken) =>
{
    var replyActivity = activity.CreateReplyActivity("You said " + activity.Text);
    await botApp.SendActivityAsync(replyActivity, cancellationToken);
};

webApp.Run();

