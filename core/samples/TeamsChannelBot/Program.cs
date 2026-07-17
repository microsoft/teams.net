// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication app = webApp.UseTeamsBotApplication();


app.OnConversationUpdate(async (context, cancellationToken) =>
    {
        Console.WriteLine($"[ConversationUpdate] Conversation updated");
    }
);

// ==================== CHANNEL EVENT HANDLERS ====================

app.OnChannelCreated(async (context, cancellationToken) =>
{
    string channelName = context.Activity.ChannelData?.Channel?.Name ?? "unknown";
    Console.WriteLine($"[ChannelCreated] Channel '{channelName}' was created");
    await context.SendAsync($"New channel created: {channelName}", cancellationToken);
});

app.OnChannelDeleted(async (context, cancellationToken) =>
{
    string channelName = context.Activity.ChannelData?.Channel?.Name ?? "unknown";
    Console.WriteLine($"[ChannelDeleted] Channel '{channelName}' was deleted");
    await context.SendAsync($"Channel deleted: {channelName}", cancellationToken);
});

app.OnChannelRenamed(async (context, cancellationToken) =>
{
    string channelName = context.Activity.ChannelData?.Channel?.Name ?? "unknown";
    Console.WriteLine($"[ChannelRenamed] Channel renamed to '{channelName}'");
    await context.SendAsync($"Channel renamed to: {channelName}", cancellationToken);
});

app.OnChannelMemberAdded(async (context, cancellationToken) =>
{
    Console.WriteLine($"[ChannelMemberAdded] Member added to channel");
    await context.SendAsync("A member was added to the channel", cancellationToken);
});

app.OnChannelMemberRemoved(async (context, cancellationToken) =>
{
    Console.WriteLine($"[ChannelMemberRemoved] Member removed from channel");
    await context.SendAsync("A member was removed from the channel", cancellationToken);
});

app.OnChannelShared(async (context, cancellationToken) =>
{
    string channelName = context.Activity.ChannelData?.Channel?.Name ?? "unknown";
    Console.WriteLine($"[ChannelShared] Channel '{channelName}' was shared");
    await context.SendAsync($"Channel shared: {channelName}", cancellationToken);
});

app.OnChannelUnshared(async (context, cancellationToken) =>
{
    string channelName = context.Activity.ChannelData?.Channel?.Name ?? "unknown";
    Console.WriteLine($"[ChannelUnshared] Channel '{channelName}' was unshared");
    await context.SendAsync($"Channel unshared: {channelName}", cancellationToken);
});

// ==================== TEAM EVENT HANDLERS ====================

app.OnTeamMemberAdded(async (context, cancellationToken) =>
{
    Console.WriteLine($"[TeamMemberAdded] Member added to team");
    await context.SendAsync("A member was added to the team", cancellationToken);
});

app.OnTeamMemberRemoved(async (context, cancellationToken) =>
{
    Console.WriteLine($"[TeamMemberRemoved] Member removed from team");
    await context.SendAsync("A member was removed from the team", cancellationToken);
});

app.OnTeamArchived((context, cancellationToken) =>
{
    string teamName = context.Activity.ChannelData?.Team?.Name ?? "unknown";
    Console.WriteLine($"[TeamArchived] Team '{teamName}' was archived");
    return Task.CompletedTask;
});

app.OnTeamDeleted((context, cancellationToken) =>
{
    string teamName = context.Activity.ChannelData?.Team?.Name ?? "unknown";
    Console.WriteLine($"[TeamDeleted] Team '{teamName}' was deleted");
    return Task.CompletedTask;
});

app.OnTeamRenamed(async (context, cancellationToken) =>
{
    string teamName = context.Activity.ChannelData?.Team?.Name ?? "unknown";
    Console.WriteLine($"[TeamRenamed] Team renamed to '{teamName}'");
    await context.SendAsync($"Team renamed to: {teamName}", cancellationToken);
});

app.OnTeamUnarchived(async (context, cancellationToken) =>
{
    string teamName = context.Activity.ChannelData?.Team?.Name ?? "unknown";
    Console.WriteLine($"[TeamUnarchived] Team '{teamName}' was unarchived");
    await context.SendAsync($"Team unarchived: {teamName}", cancellationToken);
});

webApp.Run();
