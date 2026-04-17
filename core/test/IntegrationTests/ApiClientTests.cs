// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Api.Clients;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;
using Xunit.Abstractions;

namespace IntegrationTests;

/// <summary>
/// Integration tests for <see cref="ApiClient"/> sub-clients making real API calls.
/// These tests verify that the ApiClient facade correctly delegates to core ConversationClient
/// and that Teams/Meeting-specific BotHttpClient calls work end-to-end.
/// </summary>
public class ApiClientTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _f;
    private readonly ITestOutputHelper _output;
    private readonly ApiClient _api;

    public ApiClientTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _f = fixture;
        _output = output;
        _api = _f.ScopedApiClient;
    }

    #region Activities

    [Fact]
    public async Task Activities_CreateAsync()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ApiClient.Activities.Create] at `{DateTime.UtcNow:s}`" } }
        };

        SendActivityResponse? res = await _api.Conversations.Activities.CreateAsync(_f.ConversationId, activity);

        Assert.NotNull(res);
        Assert.NotNull(res.Id);
        _output.WriteLine($"Created activity: {res.Id}");
    }

    [Fact]
    public async Task Activities_UpdateAsync()
    {
        CoreActivity original = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ApiClient.Activities.Update] Original at `{DateTime.UtcNow:s}`" } }
        };

        SendActivityResponse? sent = await _api.Conversations.Activities.CreateAsync(_f.ConversationId, original);
        Assert.NotNull(sent?.Id);

        CoreActivity updated = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ApiClient.Activities.Update] Updated at `{DateTime.UtcNow:s}`" } }
        };

        UpdateActivityResponse? res = await _api.Conversations.Activities.UpdateAsync(
            _f.ConversationId, sent.Id, updated);

        Assert.NotNull(res?.Id);
        _output.WriteLine($"Updated activity: {res.Id}");
    }

    [Fact]
    public async Task Activities_ReplyAsync()
    {
        CoreActivity original = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ApiClient.Activities.Reply] Parent at `{DateTime.UtcNow:s}`" } }
        };

        SendActivityResponse? sent = await _api.Conversations.Activities.CreateAsync(_f.ConversationId, original);
        Assert.NotNull(sent?.Id);

        CoreActivity reply = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ApiClient.Activities.Reply] Reply at `{DateTime.UtcNow:s}`" } }
        };

        SendActivityResponse? res = await _api.Conversations.Activities.ReplyAsync(
            _f.ConversationId, sent.Id, reply);

        Assert.NotNull(res);
        _output.WriteLine($"Reply activity: {res?.Id}");
    }

    [Fact]
    public async Task Activities_DeleteAsync()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ApiClient.Activities.Delete] at `{DateTime.UtcNow:s}`" } }
        };

        SendActivityResponse? sent = await _api.Conversations.Activities.CreateAsync(_f.ConversationId, activity);
        Assert.NotNull(sent?.Id);

        await Task.Delay(2000);

        await _api.Conversations.Activities.DeleteAsync(_f.ConversationId, sent.Id);
        _output.WriteLine($"Deleted activity: {sent.Id}");
    }

    #endregion

    #region Members

    [Fact]
    public async Task Members_GetAsync()
    {
        IList<ConversationAccount> members = await _api.Conversations.Members.GetAsync(_f.ConversationId);

        Assert.NotNull(members);
        Assert.NotEmpty(members);

        foreach (ConversationAccount m in members)
        {
            _output.WriteLine($"Member: {m.Id} — {m.Name}");
        }
    }

    [Fact]
    public async Task Members_GetByIdAsync()
    {
        // Get MRI-format member ID from the members list first
        IList<ConversationAccount> members = await _api.Conversations.Members.GetAsync(_f.ConversationId);
        Assert.NotEmpty(members);
        string memberId = members[0].Id!;

        ConversationAccount member = await _api.Conversations.Members.GetByIdAsync(
            _f.ConversationId, memberId);

        Assert.NotNull(member);
        Assert.Equal(memberId, member.Id);
        _output.WriteLine($"Member: {member.Id} — {member.Name}");
    }

    [Fact]
    public async Task Members_GetByIdAsync_AsTeamsConversationAccount()
    {
        // Get MRI-format member ID from the members list first
        IList<ConversationAccount> members = await _api.Conversations.Members.GetAsync(_f.ConversationId);
        Assert.NotEmpty(members);
        string memberId = members[0].Id!;

        TeamsConversationAccount member = await _api.Conversations.Members.GetByIdAsync<TeamsConversationAccount>(
            _f.ConversationId, memberId);

        Assert.NotNull(member);
        Assert.Equal(memberId, member.Id);
        _output.WriteLine($"Member: {member.Id} — {member.Name}, Email: {member.Email}, UPN: {member.UserPrincipalName}");
    }

    #endregion

    #region Reactions

    [Fact(Skip = "Reactions API returns NotFound — needs service-url scoped auth")]
    public async Task Reactions_AddAndDelete()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ApiClient.Reactions] Test at `{DateTime.UtcNow:s}`" } }
        };

        SendActivityResponse? sent = await _api.Conversations.Activities.CreateAsync(_f.ConversationId, activity);
        Assert.NotNull(sent?.Id);

        await _api.Conversations.Reactions.AddAsync(_f.ConversationId, sent.Id, "like");
        _output.WriteLine("Added 'like' reaction");

        await Task.Delay(1000);

        await _api.Conversations.Reactions.DeleteAsync(_f.ConversationId, sent.Id, "like");
        _output.WriteLine("Removed 'like' reaction");
    }

    #endregion

    #region Teams

    [Fact]
    public async Task Teams_GetByIdAsync()
    {
        Team? team = await _api.Teams.GetByIdAsync(_f.TeamId);

        Assert.NotNull(team);
        _output.WriteLine($"Team: {team.Id} — {team.Name}, Members: {team.MemberCount}, Channels: {team.ChannelCount}");
    }

    [Fact]
    public async Task Teams_GetConversationsAsync()
    {
        List<TeamsChannel>? channels = await _api.Teams.GetConversationsAsync(_f.TeamId);

        Assert.NotNull(channels);
        Assert.NotEmpty(channels);

        foreach (TeamsChannel ch in channels)
        {
            _output.WriteLine($"Channel: {ch.Id} — {ch.Name}");
        }
    }

    #endregion

    #region Meetings

    [Fact]
    public async Task Meetings_GetByIdAsync()
    {
        Meeting? meeting = await _api.Meetings.GetByIdAsync(_f.MeetingId);

        Assert.NotNull(meeting);
        _output.WriteLine($"Meeting: {meeting.Id}");
        if (meeting.Details is not null)
        {
            _output.WriteLine($"  Title: {meeting.Details.Title}, Type: {meeting.Details.Type}");
        }
    }

    [Fact(Skip = "Requires AAD object ID, not pairwise bot framework ID")]
    public async Task Meetings_GetParticipantAsync()
    {
        MeetingParticipant? participant = await _api.Meetings.GetParticipantAsync(
            _f.MeetingId, _f.UserId, _f.TenantId);

        Assert.NotNull(participant);
        _output.WriteLine($"Participant: {participant.User?.Id} — Role: {participant.Meeting?.Role}, InMeeting: {participant.Meeting?.InMeeting}");
    }

    #endregion

    #region ForServiceUrl

    [Fact]
    public async Task ForServiceUrl_CreatesScopedClient()
    {
        ApiClient scoped = _f.ApiClient.ForServiceUrl(_f.ServiceUrl);

        Assert.NotNull(scoped.Conversations);
        Assert.NotNull(scoped.Teams);
        Assert.NotNull(scoped.Meetings);
        Assert.Equal(_f.ServiceUrl, scoped.ServiceUrl);

        // Verify the scoped client can make a real call
        IList<ConversationAccount> members = await scoped.Conversations.Members.GetAsync(_f.ConversationId);
        Assert.NotNull(members);
        Assert.NotEmpty(members);
        _output.WriteLine($"ForServiceUrl scoped client retrieved {members.Count} members");
    }

    #endregion
}
