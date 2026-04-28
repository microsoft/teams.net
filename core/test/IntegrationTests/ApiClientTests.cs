// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using Microsoft.Teams.Bot.Apps.Api.Clients;
using Microsoft.Teams.Bot.Apps.Handlers.MessageExtension;
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
        _f.OutputHelper = output;
        _output = output;
        _api = _f.ScopedApiClient;
    }

    #region Activities

    [Fact(Timeout = 60000)]
    public async Task Activities_CreateAsync()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ApiClient.Activities.Create] at `{DateTime.UtcNow:s}`" } },
            From = IntegrationTestFixture.GetConversationAccountWithAgenticProperties()
        };

        SendActivityResponse? res = await _api.Conversations.Activities.CreateAsync(_f.ConversationId, activity);

        Assert.NotNull(res);
        Assert.NotNull(res.Id);
        _output.WriteLine($"Created activity: {res.Id}");
    }

    [Fact(Timeout = 60000)]
    public async Task Activities_UpdateAsync()
    {
        CoreActivity original = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ApiClient.Activities.Update] Original at `{DateTime.UtcNow:s}`" } },
            From = IntegrationTestFixture.GetConversationAccountWithAgenticProperties()
        };

        SendActivityResponse? sent = await _api.Conversations.Activities.CreateAsync(_f.ConversationId, original);
        Assert.NotNull(sent?.Id);

        CoreActivity updated = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ApiClient.Activities.Update] Updated at `{DateTime.UtcNow:s}`" } },
            From = IntegrationTestFixture.GetConversationAccountWithAgenticProperties()
        };

        UpdateActivityResponse? res = await _api.Conversations.Activities.UpdateAsync(
            _f.ConversationId, sent.Id, updated);

        Assert.NotNull(res?.Id);
        _output.WriteLine($"Updated activity: {res.Id}");
    }

    [Fact(Timeout = 60000)]
    public async Task Activities_ReplyAsync()
    {
        CoreActivity original = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ApiClient.Activities.Reply] Parent at `{DateTime.UtcNow:s}`" } },
            From = IntegrationTestFixture.GetConversationAccountWithAgenticProperties()
        };

        SendActivityResponse? sent = await _api.Conversations.Activities.CreateAsync(_f.ConversationId, original);
        Assert.NotNull(sent?.Id);

        CoreActivity reply = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ApiClient.Activities.Reply] Reply at `{DateTime.UtcNow:s}`" } },
            From = IntegrationTestFixture.GetConversationAccountWithAgenticProperties()
        };

        SendActivityResponse? res = await _api.Conversations.Activities.ReplyAsync(
            _f.ConversationId, sent.Id, reply);

        Assert.NotNull(res);
        _output.WriteLine($"Reply activity: {res?.Id}");
    }

    [Fact(Timeout = 60000)]
    public async Task Activities_DeleteAsync()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ApiClient.Activities.Delete] at `{DateTime.UtcNow:s}`" } },
            From = IntegrationTestFixture.GetConversationAccountWithAgenticProperties()
        };

        SendActivityResponse? sent = await _api.Conversations.Activities.CreateAsync(_f.ConversationId, activity);
        Assert.NotNull(sent?.Id);

        await Task.Delay(2000);

        await _api.Conversations.Activities.DeleteAsync(_f.ConversationId, sent.Id, _f.AgenticIdentity);
        _output.WriteLine($"Deleted activity: {sent.Id}");
    }

    #endregion

    #region Targeted Activities

    [Fact]
    public async Task Activities_CreateTargetedAsync()
    {
        // Targeted activities require a valid Recipient — get a real member ID
        IList<ConversationAccount> members = await _api.Conversations.Members.GetAsync(_f.ConversationId, _f.AgenticIdentity);
        Assert.NotEmpty(members);

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Recipient = new ConversationAccount { Id = members[0].Id },
            Properties = { { "text", $"[ApiClient.Activities.CreateTargeted] at `{DateTime.UtcNow:s}`" } },
            From = IntegrationTestFixture.GetConversationAccountWithAgenticProperties()
        };

        SendActivityResponse? res = await _api.Conversations.Activities.CreateTargetedAsync(_f.ConversationId, activity);

        Assert.NotNull(res);
        Assert.NotNull(res.Id);
        _output.WriteLine($"Created targeted activity: {res.Id}");
    }

    [Fact]
    public async Task Activities_UpdateTargetedAsync()
    {
        IList<ConversationAccount> members = await _api.Conversations.Members.GetAsync(_f.ConversationId, _f.AgenticIdentity);
        Assert.NotEmpty(members);

        CoreActivity original = new()
        {
            Type = ActivityType.Message,
            Recipient = new ConversationAccount { Id = members[0].Id },
            Properties = { { "text", $"[ApiClient.Activities.UpdateTargeted] Original at `{DateTime.UtcNow:s}`" } },
            From = IntegrationTestFixture.GetConversationAccountWithAgenticProperties()
        };

        SendActivityResponse? sent = await _api.Conversations.Activities.CreateTargetedAsync(_f.ConversationId, original);
        Assert.NotNull(sent?.Id);

        CoreActivity updated = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ApiClient.Activities.UpdateTargeted] Updated at `{DateTime.UtcNow:s}`" } },
            From = IntegrationTestFixture.GetConversationAccountWithAgenticProperties()
        };

        UpdateActivityResponse? res = await _api.Conversations.Activities.UpdateTargetedAsync(
            _f.ConversationId, sent.Id, updated);

        Assert.NotNull(res?.Id);
        _output.WriteLine($"Updated targeted activity: {res.Id}");
    }

    [Fact]
    public async Task Activities_DeleteTargetedAsync()
    {
        IList<ConversationAccount> members = await _api.Conversations.Members.GetAsync(_f.ConversationId, _f.AgenticIdentity);
        Assert.NotEmpty(members);

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Recipient = new ConversationAccount { Id = members[0].Id },
            Properties = { { "text", $"[ApiClient.Activities.DeleteTargeted] at `{DateTime.UtcNow:s}`" } },
            From = IntegrationTestFixture.GetConversationAccountWithAgenticProperties()
        };

        SendActivityResponse? sent = await _api.Conversations.Activities.CreateTargetedAsync(_f.ConversationId, activity);
        Assert.NotNull(sent?.Id);

        await Task.Delay(2000);

        await _api.Conversations.Activities.DeleteTargetedAsync(_f.ConversationId, sent.Id, _f.AgenticIdentity);
        _output.WriteLine($"Deleted targeted activity: {sent.Id}");
    }

    #endregion

    #region Members

    [Fact(Timeout = 60000)]
    public async Task Members_GetAsync()
    {
        IList<ConversationAccount> members = await _api.Conversations.Members.GetAsync(_f.ConversationId, _f.AgenticIdentity);

        Assert.NotNull(members);
        Assert.NotEmpty(members);

        foreach (ConversationAccount m in members)
        {
            _output.WriteLine($"Member: {m.Id} — {m.Name}");
        }
    }

    [Fact(Timeout = 60000)]
    public async Task Members_GetByIdAsync()
    {
        // Get MRI-format member ID from the members list first
        IList<ConversationAccount> members = await _api.Conversations.Members.GetAsync(_f.ConversationId, _f.AgenticIdentity);
        Assert.NotEmpty(members);
        string memberId = members[0].Id!;

        ConversationAccount member = await _api.Conversations.Members.GetByIdAsync(
            _f.ConversationId, memberId, _f.AgenticIdentity);

        Assert.NotNull(member);
        Assert.Equal(memberId, member.Id);
        _output.WriteLine($"Member: {member.Id} — {member.Name}");
    }

    [Fact(Timeout = 60000)]
    public async Task Members_GetByIdAsync_AsTeamsConversationAccount()
    {
        // Get MRI-format member ID from the members list first
        IList<ConversationAccount> members = await _api.Conversations.Members.GetAsync(_f.ConversationId, _f.AgenticIdentity);
        Assert.NotEmpty(members);
        string memberId = members[0].Id!;

        TeamsConversationAccount member = await _api.Conversations.Members.GetByIdAsync<TeamsConversationAccount>(
            _f.ConversationId, memberId, _f.AgenticIdentity);

        Assert.NotNull(member);
        Assert.Equal(memberId, member.Id);
        _output.WriteLine($"Member: {member.Id} — {member.Name}, Email: {member.Email}, UPN: {member.UserPrincipalName}");
    }

    #endregion

    #region Reactions

    [Fact]
    public async Task Reactions_AddAndDelete()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"[ApiClient.Reactions] Test at `{DateTime.UtcNow:s}`" } },
            From = IntegrationTestFixture.GetConversationAccountWithAgenticProperties()
        };

        SendActivityResponse? sent = await _api.Conversations.Activities.CreateAsync(_f.ConversationId, activity);
        Assert.NotNull(sent?.Id);

        await _api.Conversations.Reactions.AddAsync(_f.ConversationId, sent.Id, "like", _f.AgenticIdentity);
        _output.WriteLine("Added 'like' reaction");

        await Task.Delay(1000);

        await _api.Conversations.Reactions.DeleteAsync(_f.ConversationId, sent.Id, "like", _f.AgenticIdentity);
        _output.WriteLine("Removed 'like' reaction");
    }

    #endregion

    #region Teams

    [Fact(Timeout = 60000)]
    public async Task Teams_GetByIdAsync()
    {
        Team? team = await _api.Teams.GetByIdAsync(_f.TeamId, _f.AgenticIdentity);

        Assert.NotNull(team);
        _output.WriteLine($"Team: {team.Id} — {team.Name}, Members: {team.MemberCount}, Channels: {team.ChannelCount}");
    }

    [Fact(Timeout = 60000)]
    public async Task Teams_GetConversationsAsync()
    {
        List<TeamsChannel>? channels = await _api.Teams.GetConversationsAsync(_f.TeamId, _f.AgenticIdentity);

        Assert.NotNull(channels);
        Assert.NotEmpty(channels);

        foreach (TeamsChannel ch in channels)
        {
            _output.WriteLine($"Channel: {ch.Id} — {ch.Name}");
        }
    }

    #endregion

    #region Meetings

    [Fact(Timeout = 60000)]
    public async Task Meetings_GetByIdAsync()
    {
        Meeting? meeting = await _api.Meetings.GetByIdAsync(_f.MeetingId, _f.AgenticIdentity);

        Assert.NotNull(meeting);
        _output.WriteLine($"Meeting: {meeting.Id}");
        if (meeting.Details is not null)
        {
            _output.WriteLine($"  Title: {meeting.Details.Title}, Type: {meeting.Details.Type}");
        }
    }

    [Fact(Timeout = 60000)]
    public async Task Meetings_GetParticipantAsync()
    {
        // The meetings participant API requires AAD object ID, not MRI/pairwise bot framework ID.
        // Get the AAD object ID from a human member (bots don't have one).
        IList<ConversationAccount> members = await _api.Conversations.Members.GetAsync(_f.ConversationId, _f.AgenticIdentity);
        Assert.NotEmpty(members);

        string? aadObjectId = null;
        foreach (ConversationAccount m in members)
        {
            TeamsConversationAccount tm = await _api.Conversations.Members
                .GetByIdAsync<TeamsConversationAccount>(_f.ConversationId, m.Id!, _f.AgenticIdentity);
            _output.WriteLine($"Member: {tm.Name} — AadObjectId: {tm.AadObjectId ?? "(null)"}, Properties: [{string.Join(", ", tm.Properties.Keys)}]");
            if (tm.AadObjectId is not null)
            {
                aadObjectId = tm.AadObjectId;
                break;
            }
        }

        if (aadObjectId is null)
        {
            _output.WriteLine("SKIP: No members with AAD object ID found in test conversation");
            return;
        }

        MeetingParticipant? participant = await _api.Meetings.GetParticipantAsync(
            _f.MeetingId, aadObjectId, _f.TenantId, _f.AgenticIdentity);

        Assert.NotNull(participant);
        _output.WriteLine($"Participant: {participant.User?.Id} — Role: {participant.Meeting?.Role}, InMeeting: {participant.Meeting?.InMeeting}");
    }

    #endregion

    #region Bots — SignIn

    [SkippableFact(Timeout = 60000)]
    public async Task Bots_SignIn_GetUrlAsync()
    {
        Skip.If(_f.AgenticIdentity is not null, "UserTokenClient does not support agentic identity");

        string connectionName = Environment.GetEnvironmentVariable("TEST_CONNECTION_NAME")
            ?? throw new InvalidOperationException("TEST_CONNECTION_NAME not set");

        var tokenExchangeState = new
        {
            ConnectionName = connectionName,
            Conversation = new
            {
                User = new ConversationAccount { Id = _f.UserId },
            }
        };
        string tokenExchangeStateJson = JsonSerializer.Serialize(tokenExchangeState);
        string state = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenExchangeStateJson));

        string? url = await _api.Bots.SignIn.GetUrlAsync(state);

        Assert.NotNull(url);
        Assert.StartsWith("https://", url);
        _output.WriteLine($"SignIn URL: {url}");
    }

    [SkippableFact(Timeout = 60000)]
    public async Task Bots_SignIn_GetResourceAsync()
    {
        Skip.If(_f.AgenticIdentity is not null, "UserTokenClient does not support agentic identity");

        string connectionName = Environment.GetEnvironmentVariable("TEST_CONNECTION_NAME")
            ?? throw new InvalidOperationException("TEST_CONNECTION_NAME not set");

        var tokenExchangeState = new
        {
            ConnectionName = connectionName,
            Conversation = new
            {
                User = new ConversationAccount { Id = _f.UserId },
            }
        };
        string tokenExchangeStateJson = JsonSerializer.Serialize(tokenExchangeState);
        string state = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenExchangeStateJson));


        var resource = await _api.Bots.SignIn.GetResourceAsync(state);

        Assert.NotNull(resource);
        _output.WriteLine($"SignIn Resource: {resource.SignInLink}");
    }

    #endregion

    #region Users — Token

    [SkippableFact(Timeout = 60000)]
    public async Task Users_Token_GetStatusAsync()
    {
        Skip.If(_f.AgenticIdentity is not null, "UserTokenClient does not support agentic identity");

        // Get a valid member ID from the conversation
        IList<ConversationAccount> members = await _api.Conversations.Members.GetAsync(_f.ConversationId, _f.AgenticIdentity);
        Assert.NotEmpty(members);
        string userId = members[0].Id!;

        IList<GetTokenStatusResult>? statuses = await _api.Users.Token.GetStatusAsync(userId, "msteams");

        // May return null or empty if user has no token connections — that's OK
        _output.WriteLine($"Token statuses: {statuses?.Count ?? 0} connections");
        if (statuses is not null)
        {
            foreach (var s in statuses)
            {
                _output.WriteLine($"  Connection: {s.ConnectionName}, HasToken: {s.HasToken}");
            }
        }
    }

    [SkippableFact(Timeout = 60000)]
    public async Task Users_Token_GetAsync()
    {
        Skip.If(_f.AgenticIdentity is not null, "UserTokenClient does not support agentic identity");

        string connectionName = Environment.GetEnvironmentVariable("TEST_CONNECTION_NAME")
            ?? throw new InvalidOperationException("TEST_CONNECTION_NAME not set");

        IList<ConversationAccount> members = await _api.Conversations.Members.GetAsync(_f.ConversationId, _f.AgenticIdentity);
        Assert.NotEmpty(members);

        var result = await _api.Users.Token.GetAsync(members[0].Id!, connectionName, "msteams");
        _output.WriteLine($"Token: {(result is not null ? "acquired" : "not available")}");
    }

    [SkippableFact(Timeout = 60000)]
    public async Task Users_Token_SignOutAsync()
    {
        Skip.If(_f.AgenticIdentity is not null, "UserTokenClient does not support agentic identity");

        string connectionName = Environment.GetEnvironmentVariable("TEST_CONNECTION_NAME")
            ?? throw new InvalidOperationException("TEST_CONNECTION_NAME not set");

        IList<ConversationAccount> members = await _api.Conversations.Members.GetAsync(_f.ConversationId, _f.AgenticIdentity);
        Assert.NotEmpty(members);

        await _api.Users.Token.SignOutAsync(members[0].Id!, connectionName, "msteams");
        _output.WriteLine("SignOut completed");
    }

    #endregion

    #region ForServiceUrl

    [Fact(Timeout = 60000)]
    public async Task ForServiceUrl_CreatesScopedClient()
    {
        ApiClient scoped = _f.ApiClient.ForServiceUrl(_f.ServiceUrl);

        Assert.NotNull(scoped.Conversations);
        Assert.NotNull(scoped.Teams);
        Assert.NotNull(scoped.Meetings);
        Assert.Equal(_f.ServiceUrl, scoped.ServiceUrl);

        // Verify the scoped client can make a real call
        IList<ConversationAccount> members = await scoped.Conversations.Members.GetAsync(_f.ConversationId, _f.AgenticIdentity);
        Assert.NotNull(members);
        Assert.NotEmpty(members);
        _output.WriteLine($"ForServiceUrl scoped client retrieved {members.Count} members");
    }

    #endregion
}
