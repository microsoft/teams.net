// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Teams.Bot.Apps.Api.Clients;
using Microsoft.Teams.Bot.Compat;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;
using Xunit.Abstractions;
using CoreConversationAccount = Microsoft.Teams.Bot.Core.Schema.ConversationAccount;

namespace IntegrationTests;

/// <summary>
/// Integration tests for <see cref="CompatTeamsInfo"/> static methods making real API calls.
/// These tests verify that CompatTeamsInfo correctly bridges Bot Framework ITurnContext
/// to the underlying ConversationClient and ApiClient, producing valid compat types.
/// </summary>
public class CompatTeamsInfoTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _f;
    private readonly ITestOutputHelper _output;

    public CompatTeamsInfoTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _f = fixture;
        _f.OutputHelper = output;
        _output = output;
    }

    /// <summary>
    /// Creates an ITurnContext wired to real clients, simulating what CompatAdapter does.
    /// </summary>
    private TurnContext CreateTurnContext(
        string? conversationId = null,
        string? teamId = null,
        string? meetingId = null,
        string? tenantId = null)
    {
        Activity activity = new()
        {
            Type = ActivityTypes.Message,
            ServiceUrl = _f.ServiceUrl.ToString(),
            ChannelId = "msteams",
            Conversation = new Microsoft.Bot.Schema.ConversationAccount { Id = conversationId ?? _f.ConversationId },
            From = new ChannelAccount { Id = "bot" },
            Recipient = new ChannelAccount { Id = "user" },
        };

        // Set TeamsChannelData if teamId or meetingId is provided
        if (teamId != null || meetingId != null || tenantId != null)
        {
            TeamsChannelData channelData = new();
            if (teamId != null)
            {
                channelData.Team = new TeamInfo { Id = teamId };
            }

            if (meetingId != null)
            {
                channelData.Meeting = new TeamsMeetingInfo { Id = meetingId };
            }

            if (tenantId != null)
            {
                channelData.Tenant = new TenantInfo { Id = tenantId };
            }

            activity.ChannelData = channelData;
        }

        // Create a stub adapter (BotAdapter is abstract, use SimpleAdapter)
        SimpleAdapter adapter = new();
        TurnContext turnContext = new(adapter, activity);

        // Wire up CompatConnectorClient with real ConversationClient (same as CompatAdapter does)
        CompatConversations compatConversations = new(_f.ConversationClient)
        {
            ServiceUrl = _f.ServiceUrl.ToString()
        };
        CompatConnectorClient connectorClient = new(compatConversations);
        turnContext.TurnState.Add<IConnectorClient>(connectorClient);

        // Wire up scoped ApiClient (same as CompatAdapter does)
        ApiClient scopedApi = _f.ScopedApiClient;
        turnContext.TurnState.Add(scopedApi);

        return turnContext;
    }

    #region Member Methods (non-team scope)

    [Fact]
    public async Task GetMemberAsync_ReturnsTeamsChannelAccount()
    {
        // First get a valid MRI-format member ID
        ApiClient api = _f.ScopedApiClient;
        IList<CoreConversationAccount> members = await api.Conversations.Members.GetAsync(_f.ConversationId);
        Assert.NotEmpty(members);
        string memberId = members[0].Id!;

        using TurnContext ctx = CreateTurnContext();
        TeamsChannelAccount result = await CompatTeamsInfo.GetMemberAsync(ctx, memberId);

        Assert.NotNull(result);
        Assert.Equal(memberId, result.Id);
        _output.WriteLine($"GetMember: {result.Id} — {result.Name}, Email: {result.Email}, UPN: {result.UserPrincipalName}");
    }

#pragma warning disable CS0618 // Obsolete warning for GetMembersAsync
    [Fact]
    public async Task GetMembersAsync_ReturnsTeamsChannelAccounts()
    {
        using TurnContext ctx = CreateTurnContext();
        IEnumerable<TeamsChannelAccount> result = await CompatTeamsInfo.GetMembersAsync(ctx);

        Assert.NotNull(result);
        List<TeamsChannelAccount> members = [.. result];
        Assert.NotEmpty(members);

        foreach (TeamsChannelAccount m in members)
        {
            _output.WriteLine($"GetMembers: {m.Id} — {m.Name}");
        }
    }
#pragma warning restore CS0618

    [Fact]
    public async Task GetPagedMembersAsync_ReturnsPaged()
    {
        using TurnContext ctx = CreateTurnContext();
        TeamsPagedMembersResult result = await CompatTeamsInfo.GetPagedMembersAsync(ctx, pageSize: 2);

        Assert.NotNull(result);
        Assert.NotNull(result.Members);
        Assert.NotEmpty(result.Members);

        foreach (TeamsChannelAccount m in result.Members)
        {
            _output.WriteLine($"PagedMember: {m.Id} — {m.Name}");
        }

        _output.WriteLine($"ContinuationToken: {result.ContinuationToken ?? "(null)"}");
    }

    #endregion

    #region Team-scoped Member Methods

    [Fact]
    public async Task GetTeamMemberAsync_ReturnsTeamsChannelAccount()
    {
        // Get a valid MRI-format member ID from the team
        ApiClient api = _f.ScopedApiClient;
        IList<CoreConversationAccount> members = await api.Conversations.Members.GetAsync(_f.TeamId);
        Assert.NotEmpty(members);
        string memberId = members[0].Id!;

        using TurnContext ctx = CreateTurnContext(teamId: _f.TeamId);
        TeamsChannelAccount result = await CompatTeamsInfo.GetTeamMemberAsync(ctx, memberId, _f.TeamId);

        Assert.NotNull(result);
        Assert.Equal(memberId, result.Id);
        _output.WriteLine($"GetTeamMember: {result.Id} — {result.Name}, Email: {result.Email}");
    }

    [Fact]
    public async Task GetMemberAsync_WithTeamScope_DelegatesToGetTeamMember()
    {
        // When activity has TeamInfo, GetMemberAsync should delegate to GetTeamMemberAsync
        ApiClient api = _f.ScopedApiClient;
        IList<CoreConversationAccount> members = await api.Conversations.Members.GetAsync(_f.TeamId);
        Assert.NotEmpty(members);
        string memberId = members[0].Id!;

        using TurnContext ctx = CreateTurnContext(teamId: _f.TeamId);
        TeamsChannelAccount result = await CompatTeamsInfo.GetMemberAsync(ctx, memberId);

        Assert.NotNull(result);
        Assert.Equal(memberId, result.Id);
        _output.WriteLine($"GetMember (team scope): {result.Id} — {result.Name}");
    }

#pragma warning disable CS0618
    [Fact]
    public async Task GetTeamMembersAsync_ReturnsMembers()
    {
        using TurnContext ctx = CreateTurnContext(teamId: _f.TeamId);
        IEnumerable<TeamsChannelAccount> result = await CompatTeamsInfo.GetTeamMembersAsync(ctx, _f.TeamId);

        Assert.NotNull(result);
        List<TeamsChannelAccount> members = result.ToList();
        Assert.NotEmpty(members);

        foreach (TeamsChannelAccount m in members)
        {
            _output.WriteLine($"TeamMember: {m.Id} — {m.Name}");
        }
    }
#pragma warning restore CS0618

    [Fact]
    public async Task GetPagedTeamMembersAsync_ReturnsPaged()
    {
        using TurnContext ctx = CreateTurnContext(teamId: _f.TeamId);
        TeamsPagedMembersResult result = await CompatTeamsInfo.GetPagedTeamMembersAsync(ctx, _f.TeamId, pageSize: 2);

        Assert.NotNull(result);
        Assert.NotNull(result.Members);
        Assert.NotEmpty(result.Members);

        foreach (TeamsChannelAccount m in result.Members)
        {
            _output.WriteLine($"PagedTeamMember: {m.Id} — {m.Name}");
        }

        _output.WriteLine($"ContinuationToken: {result.ContinuationToken ?? "(null)"}");
    }

    #endregion

    #region Team & Channel Methods

    [Fact]
    public async Task GetTeamDetailsAsync_ReturnsDetails()
    {
        using TurnContext ctx = CreateTurnContext(teamId: _f.TeamId);
        TeamDetails result = await CompatTeamsInfo.GetTeamDetailsAsync(ctx, _f.TeamId);

        Assert.NotNull(result);
        Assert.NotNull(result.Id);
        Assert.NotNull(result.Name);
        _output.WriteLine($"TeamDetails: {result.Id} — {result.Name}, AadGroupId: {result.AadGroupId}");
    }

    [Fact]
    public async Task GetTeamDetailsAsync_InfersTeamIdFromActivity()
    {
        // When teamId is null, it should be inferred from the activity's TeamsChannelData
        using TurnContext ctx = CreateTurnContext(teamId: _f.TeamId);
        TeamDetails result = await CompatTeamsInfo.GetTeamDetailsAsync(ctx);

        Assert.NotNull(result);
        Assert.NotNull(result.Id);
        _output.WriteLine($"TeamDetails (inferred): {result.Id} — {result.Name}");
    }

    [Fact]
    public async Task GetTeamChannelsAsync_ReturnsChannels()
    {
        using TurnContext ctx = CreateTurnContext(teamId: _f.TeamId);
        IList<ChannelInfo> result = await CompatTeamsInfo.GetTeamChannelsAsync(ctx, _f.TeamId);

        Assert.NotNull(result);
        Assert.NotEmpty(result);

        foreach (ChannelInfo ch in result)
        {
            _output.WriteLine($"Channel: {ch.Id} — {ch.Name}");
        }
    }

    [Fact]
    public async Task GetTeamChannelsAsync_InfersTeamIdFromActivity()
    {
        using TurnContext ctx = CreateTurnContext(teamId: _f.TeamId);
        IList<ChannelInfo> result = await CompatTeamsInfo.GetTeamChannelsAsync(ctx);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        _output.WriteLine($"Channels (inferred): {result.Count} channels found");
    }

    #endregion

    #region Meeting Methods

    [Fact]
    public async Task GetMeetingParticipantAsync_ReturnsParticipant()
    {
        // The meetings participant API requires AAD object ID, not MRI/pairwise bot framework ID.
        // Get the AAD object ID from a human member (bots don't have one).
        ApiClient api = _f.ScopedApiClient;
        IList<CoreConversationAccount> members = await api.Conversations.Members.GetAsync(_f.ConversationId);
        Assert.NotEmpty(members);

        string? aadObjectId = null;
        foreach (CoreConversationAccount m in members)
        {
            var tm = await api.Conversations.Members
                .GetByIdAsync<Microsoft.Teams.Bot.Apps.Schema.TeamsConversationAccount>(_f.ConversationId, m.Id!);
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

        using TurnContext ctx = CreateTurnContext(meetingId: _f.MeetingId, tenantId: _f.TenantId);
        TeamsMeetingParticipant result = await CompatTeamsInfo.GetMeetingParticipantAsync(
            ctx, _f.MeetingId, aadObjectId, _f.TenantId);

        Assert.NotNull(result);
        _output.WriteLine($"Participant: {result.User?.Id} — Role: {result.Meeting?.Role}, InMeeting: {result.Meeting?.InMeeting}");
    }

    #endregion

    #region Error Cases

    [Fact]
    public async Task GetTeamDetailsAsync_ThrowsWithoutTeamScope()
    {
        // No teamId in activity and no explicit teamId parameter
        using TurnContext ctx = CreateTurnContext();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CompatTeamsInfo.GetTeamDetailsAsync(ctx));
    }

    [Fact]
    public async Task GetTeamChannelsAsync_ThrowsWithoutTeamScope()
    {
        using TurnContext ctx = CreateTurnContext();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CompatTeamsInfo.GetTeamChannelsAsync(ctx));
    }

    [Fact]
    public async Task GetMemberAsync_ThrowsWithNullUserId()
    {
        using TurnContext ctx = CreateTurnContext();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CompatTeamsInfo.GetMemberAsync(ctx, null!));
    }

    #endregion

    /// <summary>
    /// Minimal BotAdapter stub for creating TurnContext in tests.
    /// </summary>
    private sealed class SimpleAdapter : BotAdapter
    {
        public override Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public override Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
            => Task.FromResult(Array.Empty<ResourceResponse>());

        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
            => Task.FromResult(new ResourceResponse());
    }
}
