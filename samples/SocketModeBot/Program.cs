// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.SocketMode;

// Socket Mode receives activities over an outbound WebSocket, so the bot runs on a
// generic host with no web server and no public messaging endpoint.
// Socket Mode is currently only available for new bots.
HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});
builder.Services.AddTeamsBotApplication(options => options.UseSocketMode());
using IHost host = builder.Build();

TeamsBotApplication teamsApp = host.UseTeamsBotApplication();
ILogger logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("SocketModeBot");

if (teamsApp.SocketMode is { } socketMode)
{
    socketMode.Events.Ready += (_, e) =>
    {
        logger.LogInformation("[socket] Geo {Geo} ready (connectionId={ConnectionId}).", e.Geo, e.Frame.ConnectionId);
        logger.LogInformation("[socket] Aggregate status: {Status}.", socketMode.Status);
    };

    socketMode.Events.Disconnected += (_, e) =>
    {
        if (e.Terminal)
        {
            logger.LogError(
                "[socket] Geo {Geo} stopped: {Error}; check bot credentials or Socket Mode access, then restart the app.",
                e.Geo, e.Error?.Message);
        }
        else
        {
            logger.LogWarning("[socket] Geo {Geo} disconnected: {Error}; reconnecting.", e.Geo, e.Error?.Message);
        }

        logger.LogInformation("[socket] Aggregate status: {Status}.", socketMode.Status);
    };

    socketMode.Events.Reconnected += (_, e) =>
        logger.LogInformation("[socket] Geo {Geo} reconnected; inbound delivery resumed.", e.Geo);
}

teamsApp.OnMessage(async (context, cancellationToken) =>
{
    // ReplyAsync quotes the user's message above the reply.
    await context.ReplyAsync($"You said: {context.Activity.Text}", cancellationToken);
});

await host.StartAsync();

if (teamsApp.SocketMode is { } startedSocketMode)
{
    logger.LogInformation("[socket] Connected geos: {Geos}.", string.Join(", ", startedSocketMode.GeoList));
    foreach ((string geo, SocketModeStatus status) in startedSocketMode.GeoStatuses)
    {
        logger.LogInformation("[socket] Geo {Geo}: {Status}.", geo, status);
    }
}

await host.WaitForShutdownAsync();
