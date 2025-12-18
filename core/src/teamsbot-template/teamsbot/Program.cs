using Microsoft.Teams.BotApps;

var teamsApp = TeamsBotApplication.CreateBuilder().Build();

teamsApp.OnMessage = async (context, cancellationToken) =>
{
    await context.SendActivityAsync("Hello! How can I assist you today?", cancellationToken);
    await context.SendActivityAsync($"You said: {context.Activity.Text}", cancellationToken);
};

teamsApp.Run();