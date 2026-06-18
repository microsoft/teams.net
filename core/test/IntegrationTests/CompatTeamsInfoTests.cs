// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.BotBuilder;
using Microsoft.Teams.Apps.Schema;
using Xunit.Abstractions;
using TeamsChannelData = Microsoft.Bot.Schema.Teams.TeamsChannelData;

namespace IntegrationTests;

/// <summary>
/// Integration tests for <see cref="TeamsApiClient"/> static methods making real API calls.
/// These tests verify that TeamsApiClient correctly bridges Bot Framework ITurnContext
/// to the underlying ConversationClient and ApiClient, producing valid compat types.
/// </summary>
public class TeamsApiClientTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _f;
    private readonly ITestOutputHelper _output;

    public TeamsApiClientTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _f = fixture;
        _f.OutputHelper = output;
        _output = output;
    }

    private ChannelAccount CreateFromAccount()
    {
        ChannelAccount from = new() { Id = "bot" };
        if (_f.AgenticIdentity is not null)
        {
            from.Properties.Add("agenticAppId", _f.AgenticIdentity.AgenticAppId);
            from.Properties.Add("agenticUserId", _f.AgenticIdentity.AgenticUserId);
            from.Properties.Add("agenticAppBlueprintId", _f.AgenticIdentity.AgenticAppBlueprintId);
        }

        return from;
    }

    /// <summary>
    /// Creates an ITurnContext wired to real clients, simulating what TeamsBotFrameworkHttpAdapter does.
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
            From = CreateFromAccount(),
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

        // Wire up CompatConnectorClient with real ConversationClient (same as TeamsBotFrameworkHttpAdapter does)
        CompatConversations compatConversations = new(_f.ConversationClient)
        {
            ServiceUrl = _f.ServiceUrl.ToString(),
            AgenticIdentity = _f.AgenticIdentity
        };
        CompatConnectorClient connectorClient = new(compatConversations);
        turnContext.TurnState.Add<IConnectorClient>(connectorClient);

        // Wire up scoped ApiClient (same as TeamsBotFrameworkHttpAdapter does)
        ApiClient scopedApi = _f.ScopedApiClient;
        turnContext.TurnState.Add(scopedApi);

        return turnContext;
    }

    #region Member Methods (non-team scope)

    [Fact(Timeout = 5000)]
    [Trait("Category", "Members")]
    public async Task GetMemberAsync_ReturnsTeamsChannelAccount()
    {
        string memberId = _f.MemberMri1!;

        using TurnContext ctx = CreateTurnContext();
        Microsoft.Bot.Schema.Teams.TeamsChannelAccount result = await TeamsApiClient.GetMemberAsync(ctx, memberId);

        Assert.NotNull(result);
        Assert.Equal(memberId, result.Id);
        _output.WriteLine($"GetMember: {result.Id} — {result.Name}, Email: {result.Email}, UPN: {result.UserPrincipalName}");
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "Members")]
    public async Task GetMembersAsync_ReturnsTeamsChannelAccounts()
    {
        using TurnContext ctx = CreateTurnContext();
        IEnumerable<Microsoft.Bot.Schema.Teams.TeamsChannelAccount> result = await TeamsApiClient.GetMembersAsync(ctx);

        Assert.NotNull(result);
        List<Microsoft.Bot.Schema.Teams.TeamsChannelAccount> members = [.. result];
        Assert.NotEmpty(members);

        foreach (Microsoft.Bot.Schema.Teams.TeamsChannelAccount m in members)
        {
            _output.WriteLine($"GetMembers: {m.Id} — {m.Name}");
        }
    }

    [SkippableFact(Timeout = 5000)]
    [Trait("Category", "Members")]
    public async Task GetPagedMembersAsync_ReturnsPaged()
    {
        Skip.If(_f.AgenticIdentity is not null, "Paged members returns 500 with agentic identity — service limitation");
        Skip.If(_f.IsCanary, "Paged members returns empty on canary — service limitation");

        using TurnContext ctx = CreateTurnContext();
        TeamsPagedMembersResult result = await TeamsApiClient.GetPagedMembersAsync(ctx, pageSize: 2);

        Assert.NotNull(result);
        Assert.NotNull(result.Members);
        Assert.NotEmpty(result.Members);

        foreach (Microsoft.Bot.Schema.Teams.TeamsChannelAccount m in result.Members)
        {
            _output.WriteLine($"PagedMember: {m.Id} — {m.Name}");
        }

        _output.WriteLine($"ContinuationToken: {result.ContinuationToken ?? "(null)"}");
    }

    #endregion

    #region Team-scoped Member Methods

    [Fact(Timeout = 5000)]
    [Trait("Category", "Members")]
    public async Task GetTeamMemberAsync_ReturnsTeamsChannelAccount()
    {
        string memberId = _f.MemberMri1!;

        using TurnContext ctx = CreateTurnContext(teamId: _f.TeamId);
        Microsoft.Bot.Schema.Teams.TeamsChannelAccount result = await TeamsApiClient.GetTeamMemberAsync(ctx, memberId, _f.TeamId);

        Assert.NotNull(result);
        Assert.Equal(memberId, result.Id);
        _output.WriteLine($"GetTeamMember: {result.Id} — {result.Name}, Email: {result.Email}");
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "Members")]
    public async Task GetMemberAsync_WithTeamScope_DelegatesToGetTeamMember()
    {
        string memberId = _f.MemberMri1!;

        using TurnContext ctx = CreateTurnContext(teamId: _f.TeamId);
        Microsoft.Bot.Schema.Teams.TeamsChannelAccount result = await TeamsApiClient.GetMemberAsync(ctx, memberId);

        Assert.NotNull(result);
        Assert.Equal(memberId, result.Id);
        _output.WriteLine($"GetMember (team scope): {result.Id} — {result.Name}");
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "Members")]
    public async Task GetTeamMembersAsync_ReturnsMembers()
    {
        using TurnContext ctx = CreateTurnContext(teamId: _f.TeamId);
        IEnumerable<Microsoft.Bot.Schema.Teams.TeamsChannelAccount> result = await TeamsApiClient.GetTeamMembersAsync(ctx, _f.TeamId);

        Assert.NotNull(result);
        List<Microsoft.Bot.Schema.Teams.TeamsChannelAccount> members = [.. result];
        Assert.NotEmpty(members);

        foreach (Microsoft.Bot.Schema.Teams.TeamsChannelAccount m in members)
        {
            _output.WriteLine($"TeamMember: {m.Id} — {m.Name}");
        }
    }

    [SkippableFact(Timeout = 5000)]
    [Trait("Category", "Members")]
    public async Task GetPagedTeamMembersAsync_ReturnsPaged()
    {
        Skip.If(_f.AgenticIdentity is not null, "Paged members returns 500 with agentic identity — service limitation");
        Skip.If(_f.IsCanary, "Paged members returns empty on canary — service limitation");

        using TurnContext ctx = CreateTurnContext(teamId: _f.TeamId);
        TeamsPagedMembersResult result = await TeamsApiClient.GetPagedTeamMembersAsync(ctx, _f.TeamId, pageSize: 2);

        Assert.NotNull(result);
        Assert.NotNull(result.Members);
        Assert.NotEmpty(result.Members);

        foreach (Microsoft.Bot.Schema.Teams.TeamsChannelAccount m in result.Members)
        {
            _output.WriteLine($"PagedTeamMember: {m.Id} — {m.Name}");
        }

        _output.WriteLine($"ContinuationToken: {result.ContinuationToken ?? "(null)"}");
    }

    #endregion

    #region Team & Channel Methods

    [Fact(Timeout = 5000)]
    [Trait("Category", "Teams")]
    public async Task GetTeamDetailsAsync_ReturnsDetails()
    {

        using TurnContext ctx = CreateTurnContext(teamId: _f.TeamId);
        TeamDetails result = await TeamsApiClient.GetTeamDetailsAsync(ctx, _f.TeamId);

        Assert.NotNull(result);
        Assert.NotNull(result.Id);
        Assert.NotNull(result.Name);
        _output.WriteLine($"TeamDetails: {result.Id} — {result.Name}, AadGroupId: {result.AadGroupId}");
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "Teams")]
    public async Task GetTeamDetailsAsync_InfersTeamIdFromActivity()
    {

        // When teamId is null, it should be inferred from the activity's TeamsChannelData
        using TurnContext ctx = CreateTurnContext(teamId: _f.TeamId);
        TeamDetails result = await TeamsApiClient.GetTeamDetailsAsync(ctx);

        Assert.NotNull(result);
        Assert.NotNull(result.Id);
        _output.WriteLine($"TeamDetails (inferred): {result.Id} — {result.Name}");
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "Teams")]
    public async Task GetTeamChannelsAsync_ReturnsChannels()
    {

        using TurnContext ctx = CreateTurnContext(teamId: _f.TeamId);
        ConversationList result = await TeamsApiClient.GetTeamChannelsAsync(ctx, _f.TeamId);

        Assert.NotNull(result);
        Assert.NotNull(result.Conversations);
        Assert.NotEmpty(result.Conversations);

        foreach (ChannelInfo ch in result.Conversations)
        {
            _output.WriteLine($"Channel: {ch.Id} — {ch.Name}");
        }
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "Teams")]
    public async Task GetTeamChannelsAsync_InfersTeamIdFromActivity()
    {

        using TurnContext ctx = CreateTurnContext(teamId: _f.TeamId);
        ConversationList result = await TeamsApiClient.GetTeamChannelsAsync(ctx);

        Assert.NotNull(result);
        Assert.NotNull(result.Conversations);
        Assert.NotEmpty(result.Conversations);
        _output.WriteLine($"Channels (inferred): {result.Conversations.Count} channels found");
    }

    #endregion

    #region Meeting Methods

    [Fact(Timeout = 5000)]
    [Trait("Category", "Meetings")]
    public async Task GetMeetingParticipantAsync_ReturnsParticipant()
    {

        // The meetings participant API requires AAD object ID, not MRI/pairwise bot framework ID.
        // Get the AAD object ID from a human member (bots don't have one).
        ApiClient api = _f.ScopedApiClient;
        IList<Microsoft.Teams.Apps.Schema.TeamsChannelAccount?> members = await api.Conversations.Members.GetAsync(_f.ConversationId, _f.AgenticIdentity);
        Assert.NotEmpty(members);

        string? aadObjectId = null;
        foreach (Microsoft.Teams.Apps.Schema.TeamsChannelAccount? m in members)
        {
            Microsoft.Teams.Apps.Schema.TeamsChannelAccount tm = await api.Conversations.Members
                .GetByIdAsync<Microsoft.Teams.Apps.Schema.TeamsChannelAccount>(_f.ConversationId, m?.Id!, _f.AgenticIdentity);
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
        TeamsMeetingParticipant result = await TeamsApiClient.GetMeetingParticipantAsync(
            ctx, _f.MeetingId, aadObjectId, _f.TenantId);

        Assert.NotNull(result);
        _output.WriteLine($"Participant: {result.User?.Id} — Role: {result.Meeting?.Role}, InMeeting: {result.Meeting?.InMeeting}");
    }

    #endregion

    #region Error Cases

    [Fact(Timeout = 5000)]
    [Trait("Category", "ErrorHandling")]
    public async Task GetTeamDetailsAsync_ThrowsWithoutTeamScope()
    {
        // No teamId in activity and no explicit teamId parameter
        using TurnContext ctx = CreateTurnContext();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => TeamsApiClient.GetTeamDetailsAsync(ctx));
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "ErrorHandling")]
    public async Task GetTeamChannelsAsync_ThrowsWithoutTeamScope()
    {
        using TurnContext ctx = CreateTurnContext();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => TeamsApiClient.GetTeamChannelsAsync(ctx));
    }

    [Fact(Timeout = 5000)]
    [Trait("Category", "ErrorHandling")]
    public async Task GetMemberAsync_ThrowsWithNullUserId()
    {
        using TurnContext ctx = CreateTurnContext();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => TeamsApiClient.GetMemberAsync(ctx, null!));
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
