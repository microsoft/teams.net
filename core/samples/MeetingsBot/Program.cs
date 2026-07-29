// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Meetings;

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

webApp.Run();
