// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers;

var builder = TeamsBotApplication.CreateBuilder();
var app = builder.Build();


//TODO : implement next(); 
/*app.OnConversationUpdate(async (context, cancellationToken) =>
{
    Console.WriteLine($"[ConversationUpdate] Conversation updated");
}
);
;*/

// ==================== CHANNEL EVENT HANDLERS ====================

app.OnChannelCreated(async (context, cancellationToken) =>
{
    var channelName = context.Activity.ChannelData?.Channel?.Name ?? "unknown";
    Console.WriteLine($"[ChannelCreated] Channel '{channelName}' was created");
    await context.SendActivityAsync($"New channel created: {channelName}", cancellationToken);
});

app.OnChannelDeleted(async (context, cancellationToken) =>
{
    var channelName = context.Activity.ChannelData?.Channel?.Name ?? "unknown";
    Console.WriteLine($"[ChannelDeleted] Channel '{channelName}' was deleted");
    await context.SendActivityAsync($"Channel deleted: {channelName}", cancellationToken);
});

app.OnChannelRenamed(async (context, cancellationToken) =>
{
    var channelName = context.Activity.ChannelData?.Channel?.Name ?? "unknown";
    Console.WriteLine($"[ChannelRenamed] Channel renamed to '{channelName}'");
    await context.SendActivityAsync($"Channel renamed to: {channelName}", cancellationToken);
});

/*
//not able to test - no activity received
app.OnChannelRestored(async (context, cancellationToken) =>
{
    var channelName = context.Activity.ChannelData?.Channel?.Name ?? "unknown";
    Console.WriteLine($"[ChannelRestored] Channel '{channelName}' was restored");
    await context.SendActivityAsync($"Channel restored: {channelName}", cancellationToken);
});

// not able to test - can't add bot to shared channel
app.OnChannelShared(async (context, cancellationToken) =>
{
    var channelName = context.Activity.ChannelData?.Channel?.Name ?? "unknown";
    Console.WriteLine($"[ChannelShared] Channel '{channelName}' was shared");
    await context.SendActivityAsync($"Channel shared: {channelName}", cancellationToken);
});

//  not able to test - can't add bot to shared channel
app.OnChannelUnshared(async (context, cancellationToken) =>
{
    var channelName = context.Activity.ChannelData?.Channel?.Name ?? "unknown";
    Console.WriteLine($"[ChannelUnshared] Channel '{channelName}' was unshared");
    await context.SendActivityAsync($"Channel unshared: {channelName}", cancellationToken);
});

// not able to test - can't add bot to private/shared channel
app.OnChannelMemberAdded(async (context, cancellationToken) =>
{
    Console.WriteLine($"[ChannelMemberAdded] Member added to channel");
    await context.SendActivityAsync("A member was added to the channel", cancellationToken);
});

// not able to test - can't add bot to private/shared channel
app.OnChannelMemberRemoved(async (context, cancellationToken) =>
{
    Console.WriteLine($"[ChannelMemberRemoved] Member removed from channel");
    await context.SendActivityAsync("A member was removed from the channel", cancellationToken);
});
*/

// ==================== TEAM EVENT HANDLERS ====================

app.OnTeamMemberAdded(async (context, cancellationToken) =>
{
    Console.WriteLine($"[TeamMemberAdded] Member added to team");
    await context.SendActivityAsync("A member was added to the team", cancellationToken);
});

app.OnTeamMemberRemoved(async (context, cancellationToken) =>
{
    Console.WriteLine($"[TeamMemberRemoved] Member removed from team");
    await context.SendActivityAsync("A member was removed from the team", cancellationToken);
});

app.OnTeamArchived((context, cancellationToken) =>
{
    var teamName = context.Activity.ChannelData?.Team?.Name ?? "unknown";
    Console.WriteLine($"[TeamArchived] Team '{teamName}' was archived");
    return Task.CompletedTask;
});

app.OnTeamDeleted((context, cancellationToken) =>
{
    var teamName = context.Activity.ChannelData?.Team?.Name ?? "unknown";
    Console.WriteLine($"[TeamDeleted] Team '{teamName}' was deleted");
    return Task.CompletedTask;
});

app.OnTeamRenamed(async (context, cancellationToken) =>
{
    var teamName = context.Activity.ChannelData?.Team?.Name ?? "unknown";
    Console.WriteLine($"[TeamRenamed] Team renamed to '{teamName}'");
    await context.SendActivityAsync($"Team renamed to: {teamName}", cancellationToken);
});

app.OnTeamUnarchived(async (context, cancellationToken) =>
{
    var teamName = context.Activity.ChannelData?.Team?.Name ?? "unknown";
    Console.WriteLine($"[TeamUnarchived] Team '{teamName}' was unarchived");
    await context.SendActivityAsync($"Team unarchived: {teamName}", cancellationToken);
});
/*
// how to test ?
app.OnTeamHardDeleted((context, cancellationToken) =>
{
    var teamName = context.Activity.ChannelData?.Team?.Name ?? "unknown";
    Console.WriteLine($"[TeamHardDeleted] Team '{teamName}' was permanently deleted");
    return Task.CompletedTask;
});

// how to test ? Restore is unarchived 
app.OnTeamRestored(async (context, cancellationToken) =>
{
    var teamName = context.Activity.ChannelData?.Team?.Name ?? "unknown";
    Console.WriteLine($"[TeamRestored] Team '{teamName}' was restored");
    await context.SendActivityAsync($"Team restored: {teamName}", cancellationToken);
});
*/

app.Run();
