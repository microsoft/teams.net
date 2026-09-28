// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Schema;

// Socket Mode receives activities over an outbound WebSocket, so the bot runs on a
// generic host with no web server and no public messaging endpoint.
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTeamsBotApplication(options => options.UseSocketMode());
IHost host = builder.Build();

TeamsBotApplication teamsApp = host.UseTeamsSocketApplication();

teamsApp.OnMessage(async (context, cancellationToken) =>
{
    await context.SendAsync(
        new MessageActivityInput().WithText($"Echo: {context.Activity.Text}"),
        cancellationToken);
});

host.Run();
