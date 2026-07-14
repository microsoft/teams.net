// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;
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
        _f.OutputHelper = output;
        _output = output;
        _api = _f.ScopedApiClient;
    }

    private static MessageActivityInput CreateMessageActivity(string text) =>
        MessageActivityInput.CreateBuilder()
            .WithText(text)
            .Build();

    private static MessageActivityInput CreateMessageActivity(string text, ChannelAccount recipient)
    {
        MessageActivityInput activity = CreateMessageActivity(text);
        return activity;
    }

    #region Activities

    [Fact(Timeout = 5000)]
    [Trait("Category", "Activities")]
    public async Task Activities_CreateAsync()
    {
        MessageActivityInput activity = CreateMessageActivity($"[ApiClient.Activities.Create] at `{DateTime.UtcNow:s}`");

        SendActivityResponse? res = await _api.Conversations.Activities.CreateAsync(_f.ConversationId, activity);

        Assert.NotNull(res);
        Assert.NotNull(res.Id);
        _output.WriteLine($"Created activity: {res.Id}");
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "Activities")]
    public async Task Activities_UpdateAsync()
    {
        MessageActivityInput original = CreateMessageActivity($"[ApiClient.Activities.Update] Original at `{DateTime.UtcNow:s}`");

        SendActivityResponse? sent = await _api.Conversations.Activities.CreateAsync(_f.ConversationId, original);
        Assert.NotNull(sent?.Id);

        MessageActivityInput updated = CreateMessageActivity($"[ApiClient.Activities.Update] Updated at `{DateTime.UtcNow:s}`");

        UpdateActivityResponse? res = await _api.Conversations.Activities.UpdateAsync(
            _f.ConversationId, sent.Id, updated);

        Assert.NotNull(res?.Id);
        _output.WriteLine($"Updated activity: {res.Id}");
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "Activities")]
    public async Task Activities_ReplyAsync()
    {
        MessageActivityInput original = CreateMessageActivity($"[ApiClient.Activities.Reply] Parent at `{DateTime.UtcNow:s}`");

        SendActivityResponse? sent = await _api.Conversations.Activities.CreateAsync(_f.ConversationId, original);
        Assert.NotNull(sent?.Id);

        MessageActivityInput reply = CreateMessageActivity($"[ApiClient.Activities.Reply] Reply at `{DateTime.UtcNow:s}`");

        SendActivityResponse? res = await _api.Conversations.Activities.ReplyAsync(
            _f.ConversationId, sent.Id, reply);

        Assert.NotNull(res);
        _output.WriteLine($"Reply activity: {res?.Id}");
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "Activities")]
    public async Task Activities_DeleteAsync()
    {
        MessageActivityInput activity = CreateMessageActivity($"[ApiClient.Activities.Delete] at `{DateTime.UtcNow:s}`");

        SendActivityResponse? sent = await _api.Conversations.Activities.CreateAsync(_f.ConversationId, activity);
        Assert.NotNull(sent?.Id);

        await Task.Delay(2000);

        await _api.Conversations.Activities.DeleteAsync(_f.ConversationId, sent.Id);
        _output.WriteLine($"Deleted activity: {sent.Id}");
    }

    #endregion

    #region Targeted Activities

    [SkippableFact]
    [Trait("Category", "Activities")]
    public async Task Activities_CreateTargetedAsync()
    {
        Skip.If(_f.AgenticIdentity is not null, "Targeted activities return 500 with agentic identity — service limitation");

        MessageActivityInput activity = CreateMessageActivity(
            $"[ApiClient.Activities.CreateTargeted] at `{DateTime.UtcNow:s}`",
            new ChannelAccount { Id = _f.MemberMri1 });

        SendActivityResponse? res = await _api.Conversations.Activities.CreateTargetedAsync(_f.ConversationId, activity);

        Assert.NotNull(res);
        Assert.NotNull(res.Id);
        _output.WriteLine($"Created targeted activity: {res.Id}");
    }

    [SkippableFact]
    [Trait("Category", "Activities")]
    public async Task Activities_UpdateTargetedAsync()
    {
        Skip.If(_f.AgenticIdentity is not null, "Targeted activities return 500 with agentic identity — service limitation");
        MessageActivityInput original = CreateMessageActivity(
            $"[ApiClient.Activities.UpdateTargeted] Original at `{DateTime.UtcNow:s}`",
            new ChannelAccount { Id = _f.MemberMri1 });

        SendActivityResponse? sent = await _api.Conversations.Activities.CreateTargetedAsync(_f.ConversationId, original);
        Assert.NotNull(sent?.Id);

        MessageActivityInput updated = CreateMessageActivity($"[ApiClient.Activities.UpdateTargeted] Updated at `{DateTime.UtcNow:s}`");

        UpdateActivityResponse? res = await _api.Conversations.Activities.UpdateTargetedAsync(
            _f.ConversationId, sent.Id, updated);

        Assert.NotNull(res?.Id);
        _output.WriteLine($"Updated targeted activity: {res.Id}");
    }

    [SkippableFact]
    [Trait("Category", "Activities")]
    public async Task Activities_DeleteTargetedAsync()
    {
        Skip.If(_f.AgenticIdentity is not null, "Targeted activities return 500 with agentic identity — service limitation");
        MessageActivityInput activity = CreateMessageActivity(
            $"[ApiClient.Activities.DeleteTargeted] at `{DateTime.UtcNow:s}`",
            new ChannelAccount { Id = _f.MemberMri1 });

        SendActivityResponse? sent = await _api.Conversations.Activities.CreateTargetedAsync(_f.ConversationId, activity);
        Assert.NotNull(sent?.Id);

        await Task.Delay(2000);

        await _api.Conversations.Activities.DeleteTargetedAsync(_f.ConversationId, sent.Id);
        _output.WriteLine($"Deleted targeted activity: {sent.Id}");
    }

    #endregion

    #region Members

    [Fact(Timeout = 5000)]
    [Trait("Category", "Members")]
    public async Task Members_GetAsync()
    {
        IList<TeamsChannelAccount?> members = await _api.Conversations.Members.GetAsync(_f.ConversationId);

        Assert.NotNull(members);
        Assert.NotEmpty(members);

        foreach (TeamsChannelAccount? m in members.Take(5))
        {
            _output.WriteLine($"Member: {m?.Id} — {m?.Name}");
        }
    }

    [SkippableFact(Timeout = 5000)]
    [Trait("Category", "Members")]
    public async Task Members_GetPagedAsync()
    {
        Skip.If(_f.AgenticIdentity is not null, "Paged members returns 500 with agentic identity — service limitation");

        PagedTeamsMembersResult paged = await _api.Conversations.Members.GetPagedAsync(_f.ConversationId);

        Assert.NotNull(paged);
        Assert.NotEmpty(paged.Members);

        foreach (TeamsChannelAccount? m in paged.Members.Take(5))
        {
            _output.WriteLine($"Member: {m?.Id} — {m?.Name} {m?.AadObjectId}");
        }
    }


    [Fact(Timeout = 5000)]
    [Trait("Category", "Members")]
    public async Task Members_GetByIdAsync()
    {
        string memberId = _f.MemberMri1!;

        TeamsChannelAccount? member = await _api.Conversations.Members.GetByIdAsync(
            _f.ConversationId, memberId);

        Assert.NotNull(member);
        Assert.Equal(memberId, member.Id);
        _output.WriteLine($"Member: {member.Id} — {member.Name}");
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "Members")]
    public async Task Members_GetByIdAsync_AsTeamsChannelAccount()
    {
        string memberId = _f.MemberMri1!;

        TeamsChannelAccount member = await _api.Conversations.Members.GetByIdAsync<TeamsChannelAccount>(
            _f.ConversationId, memberId);

        Assert.NotNull(member);
        Assert.Equal(memberId, member.Id);
        _output.WriteLine($"Member: {member.Id} — {member.Name}, Email: {member.Email}, UPN: {member.UserPrincipalName}");
    }

    #endregion

    #region Reactions

    [SkippableFact]
    [Trait("Category", "Reactions")]
    public async Task Reactions_AddAndDelete()
    {
        Skip.If(_f.AgenticIdentity is not null, "Reactions API returns 404 with agentic identity — service limitation");
        Skip.If(_f.IsCanary, "Reactions API returns 404 on canary — service limitation");

        MessageActivityInput activity = CreateMessageActivity($"[ApiClient.Reactions] Test at `{DateTime.UtcNow:s}`");

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

    [Fact(Timeout = 5000)]
    [Trait("Category", "Teams")]
    public async Task Teams_GetByIdAsync()
    {
        Team? team = await _api.Teams.GetByIdAsync(_f.TeamId);

        Assert.NotNull(team);
        _output.WriteLine($"Team: {team.Id} — {team.Name}, Members: {team.MemberCount}, Channels: {team.ChannelCount}");
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "Teams")]
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

    [Fact(Timeout = 5000)]
    [Trait("Category", "Meetings")]
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

    [SkippableFact(Timeout = 15000)]
    [Trait("Category", "Meetings")]
    public async Task Meetings_GetParticipantAsync()
    {
        // The meetings participant API requires AAD object ID, not MRI/pairwise bot framework ID.
        // Use cached members to find one with an AAD object ID.
        string? aadObjectId = null;
        foreach (TeamsChannelAccount? m in _f.CachedMembers!)
        {
            if (m?.Id is null) continue;

            if (m.AadObjectId is not null)
            {
                aadObjectId = m.AadObjectId;
                break;
            }
            // If not available on the cached list, fetch full details for this member
            TeamsChannelAccount tm = await _api.Conversations.Members
                .GetByIdAsync<TeamsChannelAccount>(_f.ConversationId, m.Id);
            if (tm.AadObjectId is not null)
            {
                aadObjectId = tm.AadObjectId;
                break;
            }
        }

        Skip.If(aadObjectId is null, "No members with AAD object ID found in test conversation");

        MeetingParticipant? participant = await _api.Meetings.GetParticipantAsync(
            _f.MeetingId, aadObjectId!, _f.TenantId);

        Assert.NotNull(participant);
        _output.WriteLine($"Participant: {participant.User?.Id} — Role: {participant.Meeting?.Role}, InMeeting: {participant.Meeting?.InMeeting}");
    }

    #endregion

    #region Users — SignIn

    [SkippableFact(Timeout = 5000)]
    [Trait("Category", "Users")]
    public async Task Users_GetSignInUrlAsync()
    {
        Skip.If(_f.AgenticIdentity is not null, "UserTokenClient does not support agentic identity");

        string connectionName = Environment.GetEnvironmentVariable("TEST_CONNECTION_NAME")
            ?? throw new InvalidOperationException("TEST_CONNECTION_NAME not set");

        var tokenExchangeState = new
        {
            ConnectionName = connectionName,
            Conversation = new
            {
                User = new ChannelAccount { Id = _f.UserId },
            }
        };
        string tokenExchangeStateJson = JsonSerializer.Serialize(tokenExchangeState);
        string state = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenExchangeStateJson));

        string? url = await _api.UserToken.GetSignInUrlAsync(state);

        Assert.NotNull(url);
        Assert.StartsWith("https://", url);
        _output.WriteLine($"SignIn URL: {url}");
    }

    [SkippableFact(Timeout = 5000)]
    [Trait("Category", "Users")]
    public async Task Users_GetSignInResourceAsync()
    {
        Skip.If(_f.AgenticIdentity is not null, "UserTokenClient does not support agentic identity");

        string connectionName = Environment.GetEnvironmentVariable("TEST_CONNECTION_NAME")
            ?? throw new InvalidOperationException("TEST_CONNECTION_NAME not set");

        var tokenExchangeState = new
        {
            ConnectionName = connectionName,
            Conversation = new
            {
                User = new ChannelAccount { Id = _f.UserId },
            }
        };
        string tokenExchangeStateJson = JsonSerializer.Serialize(tokenExchangeState);
        string state = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenExchangeStateJson));


        GetSignInResourceResult? resource = await _api.UserToken.GetSignInResourceAsync(state);

        Assert.NotNull(resource);
        _output.WriteLine($"SignIn Resource: {resource.SignInLink}");
    }

    #endregion

    #region Users — Token

    [SkippableFact(Timeout = 5000)]
    [Trait("Category", "Users")]
    public async Task Users_Token_GetStatusAsync()
    {
        Skip.If(_f.AgenticIdentity is not null, "UserTokenClient does not support agentic identity");

        string userId = _f.MemberMri1!;

        IList<GetTokenStatusResult>? statuses = await _api.UserToken.GetStatusAsync(userId, "msteams");

        // May return null or empty if user has no token connections — that's OK
        _output.WriteLine($"Token statuses: {statuses?.Count ?? 0} connections");
        if (statuses is not null)
        {
            foreach (GetTokenStatusResult s in statuses)
            {
                _output.WriteLine($"  Connection: {s.ConnectionName}, HasToken: {s.HasToken}");
            }
        }
    }

    [SkippableFact(Timeout = 5000)]
    [Trait("Category", "Users")]
    public async Task Users_Token_GetAsync()
    {
        Skip.If(_f.AgenticIdentity is not null, "UserTokenClient does not support agentic identity");

        string connectionName = Environment.GetEnvironmentVariable("TEST_CONNECTION_NAME")
            ?? throw new InvalidOperationException("TEST_CONNECTION_NAME not set");

        GetTokenResult? result = await _api.UserToken.GetAsync(_f.MemberMri1!, connectionName, "msteams");
        _output.WriteLine($"Token: {(result is not null ? "acquired" : "not available")}");
    }

    [SkippableFact(Timeout = 5000)]
    [Trait("Category", "Users")]
    public async Task Users_Token_SignOutAsync()
    {
        Skip.If(_f.AgenticIdentity is not null, "UserTokenClient does not support agentic identity");

        string connectionName = Environment.GetEnvironmentVariable("TEST_CONNECTION_NAME")
            ?? throw new InvalidOperationException("TEST_CONNECTION_NAME not set");

        await _api.UserToken.SignOutAsync(_f.MemberMri1!, connectionName, "msteams");
        _output.WriteLine("SignOut completed");
    }

    #endregion

    #region ForServiceUrl

    [Fact(Timeout = 5000)]
    [Trait("Category", "Client")]
    public async Task ForServiceUrl_CreatesScopedClient()
    {
        ApiClient scoped = _f.ApiClient.ForServiceUrl(_f.ServiceUrl).ForAgenticIdentity(_f.AgenticIdentity);

        Assert.NotNull(scoped.Conversations);
        Assert.NotNull(scoped.Teams);
        Assert.NotNull(scoped.Meetings);
        Assert.Equal(_f.ServiceUrl, scoped.ServiceUrl);

        // Verify the scoped client can make a real call
        IList<TeamsChannelAccount?> members = await scoped.Conversations.Members.GetAsync(_f.ConversationId);
        Assert.NotNull(members);
        Assert.NotEmpty(members);
        _output.WriteLine($"ForServiceUrl scoped client retrieved {members.Count} members");
    }

    #endregion
}
