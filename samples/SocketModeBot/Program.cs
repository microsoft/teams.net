// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Apps;

// Socket Mode receives activities over an outbound WebSocket, so the bot runs on a
// generic host with no web server and no public messaging endpoint.
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTeamsBotApplication(options => options.UseSocketMode(socket =>
{
    // Socket Mode is only available on the canary ring for now.
    socket.NegotiateBaseUrl = new Uri("https://canary.botapi.skype.com");
}));
IHost host = builder.Build();

TeamsBotApplication teamsApp = host.UseTeamsBotApplication(socket: true);

teamsApp.OnMessage(async (context, cancellationToken) =>
{
    // ReplyAsync quotes the user's message above the reply.
    await context.ReplyAsync($"You said: {context.Activity.Text}", cancellationToken);
});

host.Run();
