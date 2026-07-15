// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

// ==================== MEETING HANDLERS ====================

teamsApp.OnMeetingStart(async (context, cancellationToken) =>
{
    MeetingStartValue? meeting = context.Activity.Value;
    Console.WriteLine($"[MeetingStart] Title: {meeting?.Title}");
    await context.SendAsync($"Meeting started: **{meeting?.Title}**", cancellationToken);
});

teamsApp.OnMeetingEnd(async (context, cancellationToken) =>
{
    MeetingEndValue? meeting = context.Activity.Value;
    Console.WriteLine($"[MeetingEnd] Title: {meeting?.Title}, EndTime: {meeting?.EndTime:u}");
    await context.SendAsync($"Meeting ended: **{meeting?.Title}**\nEnd time: {meeting?.EndTime:u}", cancellationToken);
});

teamsApp.OnMeetingJoin(async (context, cancellationToken) =>
{
    IList<MeetingParticipantMember> members = context.Activity.Value?.Members ?? [];
    string names = string.Join(", ", members.Select(m => m.User.Name ?? m.User.Id));
    Console.WriteLine($"[MeetingParticipantJoin] Members: {names}");
    await context.SendAsync($"Participant(s) joined: {names}", cancellationToken);
});

teamsApp.OnMeetingLeave(async (context, cancellationToken) =>
{
    IList<MeetingParticipantMember> members = context.Activity.Value?.Members ?? [];
    string names = string.Join(", ", members.Select(m => m.User.Name ?? m.User.Id));
    Console.WriteLine($"[MeetingParticipantLeave] Members: {names}");
    await context.SendAsync($"Participant(s) left: {names}", cancellationToken);
});

//TODO : review if we can trigger these
// ==================== COMMAND HANDLERS ====================
/*

teamsApp.OnCommand(async (context, cancellationToken) =>
{
    var commandId = context.Activity.Value?.CommandId ?? "unknown";
    Console.WriteLine($"[Command] CommandId: {commandId}");
    await context.SendAsync($"Received command: **{commandId}**", cancellationToken);
});

teamsApp.OnCommandResult(async (context, cancellationToken) =>
{
    var commandId = context.Activity.Value?.CommandId ?? "unknown";
    var error = context.Activity.Value?.Error;
    Console.WriteLine($"[CommandResult] CommandId: {commandId}, HasError: {error is not null}");

    if (error is not null)
        await context.SendAsync($"Command **{commandId}** failed: {error.Message}", cancellationToken);
    else
        await context.SendAsync($"Command **{commandId}** completed successfully.", cancellationToken);
});
*/
webApp.Run();
