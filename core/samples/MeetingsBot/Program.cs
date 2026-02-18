// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Schema;

var builder = TeamsBotApplication.CreateBuilder(args);
var teamsApp = builder.Build();

// ==================== MEETING HANDLERS ====================

teamsApp.OnMeetingStart(async (context, cancellationToken) =>
{
    var meeting = context.Activity.Value;
    Console.WriteLine($"[MeetingStart] Title: {meeting?.Title}");
    await context.SendActivityAsync($"Meeting started: **{meeting?.Title}**", cancellationToken);
});

teamsApp.OnMeetingEnd(async (context, cancellationToken) =>
{
    var meeting = context.Activity.Value;
    Console.WriteLine($"[MeetingEnd] Title: {meeting?.Title}, EndTime: {meeting?.EndTime:u}");
    await context.SendActivityAsync($"Meeting ended: **{meeting?.Title}**\nEnd time: {meeting?.EndTime:u}", cancellationToken);
});

teamsApp.OnMeetingParticipantJoin(async (context, cancellationToken) =>
{
    var members = context.Activity.Value?.Members ?? [];
    var names = string.Join(", ", members.Select(m => m.User.Name ?? m.User.Id));
    Console.WriteLine($"[MeetingParticipantJoin] Members: {names}");
    await context.SendActivityAsync($"Participant(s) joined: {names}", cancellationToken);
});

teamsApp.OnMeetingParticipantLeave(async (context, cancellationToken) =>
{
    var members = context.Activity.Value?.Members ?? [];
    var names = string.Join(", ", members.Select(m => m.User.Name ?? m.User.Id));
    Console.WriteLine($"[MeetingParticipantLeave] Members: {names}");
    await context.SendActivityAsync($"Participant(s) left: {names}", cancellationToken);
});

//TODO : review if we can trigger these
// ==================== COMMAND HANDLERS ====================
/*

teamsApp.OnCommand(async (context, cancellationToken) =>
{
    var commandId = context.Activity.Value?.CommandId ?? "unknown";
    Console.WriteLine($"[Command] CommandId: {commandId}");
    await context.SendActivityAsync($"Received command: **{commandId}**", cancellationToken);
});

teamsApp.OnCommandResult(async (context, cancellationToken) =>
{
    var commandId = context.Activity.Value?.CommandId ?? "unknown";
    var error = context.Activity.Value?.Error;
    Console.WriteLine($"[CommandResult] CommandId: {commandId}, HasError: {error is not null}");

    if (error is not null)
        await context.SendActivityAsync($"Command **{commandId}** failed: {error.Message}", cancellationToken);
    else
        await context.SendActivityAsync($"Command **{commandId}** completed successfully.", cancellationToken);
});
*/
teamsApp.Run();
