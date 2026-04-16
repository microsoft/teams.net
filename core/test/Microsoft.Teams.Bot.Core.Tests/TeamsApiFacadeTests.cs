// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Connector;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;
using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Api;
using Microsoft.Teams.Bot.Apps.Schema;
using Xunit.Abstractions;

namespace Microsoft.Bot.Core.Tests;

/// <summary>
/// Integration tests for the TeamsApi facade.
/// These tests verify that the hierarchical API facade correctly delegates to underlying clients.
/// </summary>
public class TeamsApiFacadeTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly TeamsBotApplication _teamsBotApplication;
    private readonly Uri _serviceUrl;
    private readonly string _conversationId;
    private readonly ConversationAccount _recipient = new ConversationAccount();
    private readonly AgenticIdentity? _agenticIdentity;

    private string? _resolvedUserMri;

    /// <summary>
    /// Resolves the pairwise-encrypted MRI for the test user by querying conversation members.
    /// The Bot Framework API returns member IDs in pairwise MRI format (29:1aK9...) which differs
    /// from the AAD-based format (29:&lt;aad-guid&gt;) used in TEST_USER_ID.
    /// </summary>
    private async Task<string> ResolveUserMriAsync()
    {
        if (_resolvedUserMri != null) return _resolvedUserMri;

        string testUserId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID not set");
        string aadUserId = testUserId.Replace("29:", "");

        IList<ConversationAccount> members = await _teamsBotApplication.Api.Conversations.Members.GetAllAsync(
            _conversationId, _serviceUrl, _agenticIdentity, cancellationToken: CancellationToken.None);

        // Try matching by aadObjectId or objectId in properties
        ConversationAccount? match = members.FirstOrDefault(m =>
            (m.Properties.TryGetValue("aadObjectId", out object? aadOid) && string.Equals(aadOid?.ToString(), aadUserId, StringComparison.OrdinalIgnoreCase)) ||
            (m.Properties.TryGetValue("objectId", out object? oid) && string.Equals(oid?.ToString(), aadUserId, StringComparison.OrdinalIgnoreCase)));

        _resolvedUserMri = match?.Id ?? throw new InvalidOperationException(
            $"Could not resolve pairwise MRI for AAD user {aadUserId} in conversation {_conversationId}. " +
            $"Found {members.Count} members. Properties on first member: {string.Join(", ", members.First().Properties.Keys)}");

        return _resolvedUserMri;
    }

    private async Task<string> SendBatchAndGetOperationIdAsync()
    {
        string tenantId = Environment.GetEnvironmentVariable("TEST_TENANTID") ?? throw new InvalidOperationException("TEST_TENANTID not set");
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID not set");

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Batch for operation state test at `{DateTime.UtcNow:s}`" } }
        };

        string userId2 = Environment.GetEnvironmentVariable("TEST_USER_ID_2") ?? userId;
        IList<TeamMember> members =
        [
            new TeamMember(userId),
            new TeamMember(userId2),
            new TeamMember("29:placeholder-3"),
            new TeamMember("29:placeholder-4"),
            new TeamMember("29:placeholder-5"),
        ];

        return await _teamsBotApplication.Api.Batch.SendToUsersAsync(
            activity, members, tenantId, _serviceUrl, _agenticIdentity,
            cancellationToken: CancellationToken.None);
    }

    public TeamsApiFacadeTests(ITestOutputHelper outputHelper)
    {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddEnvironmentVariables();

        IConfiguration configuration = builder.Build();

        ServiceCollection services = new();
        services.AddLogging((builder) => {
            builder.AddXUnit(outputHelper);
            builder.AddFilter("System.Net", LogLevel.Warning);
            builder.AddFilter("Microsoft.Identity", LogLevel.Error);
            builder.AddFilter("Microsoft.Teams", LogLevel.Information);
        });
        services.AddSingleton(configuration);
        services.AddHttpContextAccessor();
        services.AddTeamsBotApplication();
        _serviceProvider = services.BuildServiceProvider();
        _teamsBotApplication = _serviceProvider.GetRequiredService<TeamsBotApplication>();
        _serviceUrl = new Uri(Environment.GetEnvironmentVariable("TEST_SERVICEURL") ?? "https://smba.trafficmanager.net/teams/");
        _conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");
        string agenticAppBlueprintId = Environment.GetEnvironmentVariable("AzureAd__ClientId") ?? throw new InvalidOperationException("AzureAd__ClientId environment variable not set");
        string? agenticAppId = Environment.GetEnvironmentVariable("TEST_AGENTIC_APPID");
        string? agenticUserId = Environment.GetEnvironmentVariable("TEST_AGENTIC_USERID");

        _agenticIdentity = null;
        if (!string.IsNullOrEmpty(agenticAppId) && !string.IsNullOrEmpty(agenticUserId))
        {
            _recipient.Properties.Add("agenticAppBlueprintId", agenticAppBlueprintId);
            _recipient.Properties.Add("agenticAppId", agenticAppId);
            _recipient.Properties.Add("agenticUserId", agenticUserId);
            _agenticIdentity = AgenticIdentity.FromProperties(_recipient.Properties);
        }
    }

    [Fact]
    public void Api_ReturnsTeamsApiInstance()
    {
        TeamsApi api = _teamsBotApplication.Api;

        Assert.NotNull(api);
    }

    [Fact]
    public void Api_ReturnsSameInstance()
    {
        TeamsApi api1 = _teamsBotApplication.Api;
        TeamsApi api2 = _teamsBotApplication.Api;

        Assert.Same(api1, api2);
    }

    [Fact]
    public void Api_HasAllSubApis()
    {
        TeamsApi api = _teamsBotApplication.Api;

        Assert.NotNull(api.Conversations);
        Assert.NotNull(api.Users);
        Assert.NotNull(api.Teams);
        Assert.NotNull(api.Meetings);
        Assert.NotNull(api.Batch);
    }

    [Fact]
    public void Api_Conversations_HasActivitiesAndMembers()
    {
        Assert.NotNull(_teamsBotApplication.Api.Conversations.Activities);
        Assert.NotNull(_teamsBotApplication.Api.Conversations.Members);
    }

    [Fact]
    public void Api_Users_HasToken()
    {
        Assert.NotNull(_teamsBotApplication.Api.Users.Token);
    }

    [Fact]
    public async Task Api_Teams_GetByIdAsync()
    {
        string teamId = Environment.GetEnvironmentVariable("TEST_TEAMID") ?? throw new InvalidOperationException("TEST_TEAMID environment variable not set");

        TeamDetails result = await _teamsBotApplication.Api.Teams.GetByIdAsync(
            teamId,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Id);

        Console.WriteLine($"Team details via Api.Teams.GetByIdAsync:");
        Console.WriteLine($"  - Id: {result.Id}");
        Console.WriteLine($"  - Name: {result.Name}");
    }

    [Fact]
    public async Task Api_Teams_GetChannelsAsync()
    {
        string teamId = Environment.GetEnvironmentVariable("TEST_TEAMID") ?? throw new InvalidOperationException("TEST_TEAMID environment variable not set");

        ChannelList result = await _teamsBotApplication.Api.Teams.GetChannelsAsync(
            teamId,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Channels);
        Assert.NotEmpty(result.Channels);

        Console.WriteLine($"Found {result.Channels.Count} channels via Api.Teams.GetChannelsAsync:");
        foreach (var channel in result.Channels)
        {
            Console.WriteLine($"  - Id: {channel.Id}, Name: {channel.Name}");
        }
    }

    [Fact]
    public async Task Api_Teams_GetByIdAsync_WithActivityContext()
    {
        string teamId = Environment.GetEnvironmentVariable("TEST_TEAMID") ?? throw new InvalidOperationException("TEST_TEAMID environment variable not set");

        TeamsActivity activity = new()
        {
            ServiceUrl = _serviceUrl,
            From = TeamsConversationAccount.FromConversationAccount(_recipient),
            ChannelData = new TeamsChannelData { Team = new Team { Id = teamId } }
        };

        TeamDetails result = await _teamsBotApplication.Api.Teams.GetByIdAsync(
            activity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Id);

        Console.WriteLine($"Team details via Api.Teams.GetByIdAsync with activity context:");
        Console.WriteLine($"  - Id: {result.Id}");
    }

    [Fact]
    public async Task Api_Conversations_Activities_SendAsync()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Message via Api.Conversations.Activities.SendAsync at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            Conversation = new(_conversationId),
            From = _recipient
        };

        SendActivityResponse? res = await _teamsBotApplication.Api.Conversations.Activities.SendAsync(
            activity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(res);
        Assert.NotNull(res.Id);

        Console.WriteLine($"Sent activity via Api.Conversations.Activities.SendAsync: {res.Id}");
    }


    [Trait("Category", "needs-service-url")]
    [Fact]
    public async Task Api_Conversations_Activities_Send_Update_DeleteTMAsync()
    {
        string userId = await ResolveUserMriAsync();

        CoreActivity activity = CoreActivity.CreateBuilder()
            .WithType(ActivityType.Message)
            .WithServiceUrl(_serviceUrl)
            .WithConversation(new(_conversationId))
            .WithFrom(_recipient)
            .WithRecipient(new ConversationAccount() { Id = userId }, isTargeted: true)
            .WithProperty("text", $"TM Message via Api.Conversations.Activities.SendAsync at `{DateTime.UtcNow:s}`")
            .Build();

        SendActivityResponse? res = await _teamsBotApplication.Api.Conversations.Activities.SendAsync(
            activity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(res);
        Assert.NotNull(res.Id);

        Console.WriteLine($"Sent activity via Api.Conversations.Activities.SendAsync: {res.Id}");

        await Task.Delay(2000);

        await _teamsBotApplication.Api.Conversations.Activities.UpdateTargetedAsync(
            _conversationId,
            res.Id,
            CoreActivity.CreateBuilder()
                .WithServiceUrl(_serviceUrl)
                .WithProperty("text", $"TM Updated Message via Api.Conversations.Activities.UpdateAsync at `{DateTime.UtcNow:s}`")
                .Build(),
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        await Task.Delay(2000);
        await _teamsBotApplication.Api.Conversations.Activities.DeleteTargetedAsync(
            _conversationId,
            res.Id,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);
    }

    [Fact]
    public async Task Api_Conversations_Activities_UpdateAsync()
    {
        // First send an activity
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Original message via Api at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            Conversation = new(_conversationId),
            From = _recipient
        };

        SendActivityResponse? sendResponse = await _teamsBotApplication.Api.Conversations.Activities.SendAsync(activity);
        Assert.NotNull(sendResponse?.Id);

        // Now update the activity
        CoreActivity updatedActivity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Updated message via Api.Conversations.Activities.UpdateAsync at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            From = _recipient
        };

        UpdateActivityResponse updateResponse = await _teamsBotApplication.Api.Conversations.Activities.UpdateAsync(
            _conversationId,
            sendResponse.Id,
            updatedActivity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(updateResponse);
        Assert.NotNull(updateResponse.Id);

        Console.WriteLine($"Updated activity via Api.Conversations.Activities.UpdateAsync: {updateResponse.Id}");
    }

    [Fact]
    public async Task Api_Conversations_Activities_DeleteAsync()
    {
        // First send an activity
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Message to delete via Api at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            Conversation = new(_conversationId),
            From = _recipient
        };

        SendActivityResponse? sendResponse = await _teamsBotApplication.Api.Conversations.Activities.SendAsync(activity);
        Assert.NotNull(sendResponse?.Id);

        // Wait a bit before deleting
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Now delete the activity
        await _teamsBotApplication.Api.Conversations.Activities.DeleteAsync(
            _conversationId,
            sendResponse.Id,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Console.WriteLine($"Deleted activity via Api.Conversations.Activities.DeleteAsync: {sendResponse.Id}");
    }

    [Fact]
    public async Task Api_Conversations_Activities_GetMembersAsync()
    {
        // First send an activity
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Message for GetMembersAsync test at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            Conversation = new(_conversationId),
            From = _recipient
        };

        SendActivityResponse? sendResponse = await _teamsBotApplication.Api.Conversations.Activities.SendAsync(activity);
        Assert.NotNull(sendResponse?.Id);

        // Now get activity members
        IList<ConversationAccount> members = await _teamsBotApplication.Api.Conversations.Activities.GetMembersAsync(
            _conversationId,
            sendResponse.Id,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(members);
        Assert.NotEmpty(members);

        Console.WriteLine($"Found {members.Count} activity members via Api.Conversations.Activities.GetMembersAsync:");
        foreach (var member in members)
        {
            Console.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
        }
    }

    [Fact]
    public async Task Api_Conversations_Members_GetAllAsync()
    {
        IList<ConversationAccount> members = await _teamsBotApplication.Api.Conversations.Members.GetAllAsync(
            _conversationId,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(members);
        Assert.NotEmpty(members);

        Console.WriteLine($"Found {members.Count} conversation members via Api.Conversations.Members.GetAllAsync:");
        foreach (var member in members)
        {
            Console.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
        }
    }

    [Fact]
    public async Task Api_Conversations_Members_GetAllAsync_WithActivityContext()
    {
        TeamsActivity activity = new()
        {
            ServiceUrl = _serviceUrl,
            Conversation = new TeamsConversation { Id = _conversationId },
            From = TeamsConversationAccount.FromConversationAccount(_recipient)
        };

        IList<ConversationAccount> members = await _teamsBotApplication.Api.Conversations.Members.GetAllAsync(
            activity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(members);
        Assert.NotEmpty(members);

        Console.WriteLine($"Found {members.Count} members via Api.Conversations.Members.GetAllAsync with activity context");
    }

    [Fact]
    public async Task Api_Conversations_Members_GetByIdAsync()
    {
        string userId = await ResolveUserMriAsync();

        ConversationAccount member = await _teamsBotApplication.Api.Conversations.Members.GetByIdAsync(
            _conversationId,
            userId,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(member);
        Assert.NotNull(member.Id);

        Console.WriteLine($"Found member via Api.Conversations.Members.GetByIdAsync:");
        Console.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
    }

    [Fact]
    public async Task Api_Conversations_Members_GetPagedAsync()
    {
        PagedMembersResult result = await _teamsBotApplication.Api.Conversations.Members.GetPagedAsync(
            _conversationId,
            _serviceUrl,
            5,
            null,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Members);
        Assert.NotEmpty(result.Members);

        Console.WriteLine($"Found {result.Members.Count} members via Api.Conversations.Members.GetPagedAsync");
    }

    [Trait("Category", "needs-meeting-context")]
    [Fact]
    public async Task Api_Meetings_GetByIdAsync()
    {
        string meetingId = Environment.GetEnvironmentVariable("TEST_MEETINGID") ?? throw new InvalidOperationException("TEST_MEETINGID environment variable not set");

        MeetingInfo result = await _teamsBotApplication.Api.Meetings.GetByIdAsync(
            meetingId,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);

        Console.WriteLine($"Meeting info via Api.Meetings.GetByIdAsync:");
        if (result.Details != null)
        {
            Console.WriteLine($"  - Title: {result.Details.Title}");
            Console.WriteLine($"  - Type: {result.Details.Type}");
        }
    }

    [Trait("Category", "needs-meeting-context")]
    [Fact]
    public async Task Api_Meetings_GetParticipantAsync()
    {
        string meetingId = Environment.GetEnvironmentVariable("TEST_MEETINGID") ?? throw new InvalidOperationException("TEST_MEETINGID environment variable not set");
        // Meeting participant API expects the AAD object ID, not the 29: MRI
        string participantId = (Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set")).Replace("29:", "");
        string tenantId = Environment.GetEnvironmentVariable("TEST_TENANTID") ?? throw new InvalidOperationException("TEST_TENANTID environment variable not set");

        MeetingParticipant result = await _teamsBotApplication.Api.Meetings.GetParticipantAsync(
            meetingId,
            participantId,
            tenantId,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);

        Console.WriteLine($"Participant info via Api.Meetings.GetParticipantAsync:");
        if (result.User != null)
        {
            Console.WriteLine($"  - User Id: {result.User.Id}");
            Console.WriteLine($"  - User Name: {result.User.Name}");
        }
    }

    [Fact]
    public async Task Api_Batch_GetStateAsync_FailsWithInvalidOperationId()
    {
        await Assert.ThrowsAsync<HttpRequestException>(()
            => _teamsBotApplication.Api.Batch.GetStateAsync("invalid-operation-id", _serviceUrl, _agenticIdentity));
    }

    [Fact]
    public async Task Api_Teams_GetByIdAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Teams.GetByIdAsync((TeamsActivity)null!));
    }

    [Fact]
    public async Task Api_Teams_GetChannelsAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Teams.GetChannelsAsync((TeamsActivity)null!));
    }

    [Fact]
    public async Task Api_Conversations_Members_GetAllAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Conversations.Members.GetAllAsync((TeamsActivity)null!));
    }

    [Fact]
    public async Task Api_Conversations_Members_GetByIdAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Conversations.Members.GetByIdAsync((TeamsActivity)null!, "user-id"));
    }

    [Fact]
    public async Task Api_Conversations_Members_GetPagedAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Conversations.Members.GetPagedAsync((TeamsActivity)null!));
    }

    [Fact]
    public async Task Api_Conversations_Members_DeleteAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Conversations.Members.DeleteAsync((TeamsActivity)null!, "member-id"));
    }

    [Fact]
    public async Task Api_Meetings_GetByIdAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Meetings.GetByIdAsync("meeting-id", (TeamsActivity)null!));
    }

    [Fact]
    public async Task Api_Meetings_GetParticipantAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Meetings.GetParticipantAsync("meeting-id", "participant-id", (TeamsActivity)null!));
    }

    [Fact]
    public async Task Api_Meetings_SendNotificationAsync_ThrowsOnNullActivity()
    {
        var notification = new TargetedMeetingNotification();
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Meetings.SendNotificationAsync("meeting-id", notification, (TeamsActivity)null!));
    }

    [Fact]
    public async Task Api_Batch_GetStateAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Batch.GetStateAsync("operation-id", (TeamsActivity)null!));
    }

    [Fact]
    public async Task Api_Batch_GetFailedEntriesAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Batch.GetFailedEntriesAsync("operation-id", (TeamsActivity)null!));
    }

    [Fact]
    public async Task Api_Batch_CancelAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Batch.CancelAsync("operation-id", (TeamsActivity)null!));
    }

    [Fact]
    public async Task Api_Batch_SendToUsersAsync_ThrowsOnNullActivity()
    {
        var activity = new CoreActivity { Type = ActivityType.Message };
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Batch.SendToUsersAsync(activity, [new TeamMember("id")], (TeamsActivity)null!));
    }

    [Fact]
    public async Task Api_Batch_SendToTenantAsync_ThrowsOnNullActivity()
    {
        var activity = new CoreActivity { Type = ActivityType.Message };
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Batch.SendToTenantAsync(activity, (TeamsActivity)null!));
    }

    [Fact]
    public async Task Api_Batch_SendToTeamAsync_ThrowsOnNullActivity()
    {
        var activity = new CoreActivity { Type = ActivityType.Message };
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Batch.SendToTeamAsync(activity, "team-id", (TeamsActivity)null!));
    }

    [Fact]
    public async Task Api_Batch_SendToChannelsAsync_ThrowsOnNullActivity()
    {
        var activity = new CoreActivity { Type = ActivityType.Message };
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Batch.SendToChannelsAsync(activity, [new TeamMember("id")], (TeamsActivity)null!));
    }

    [Fact]
    public async Task Api_Users_Token_GetAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Users.Token.GetAsync((TeamsActivity)null!, "connection-name"));
    }

    [Fact]
    public async Task Api_Users_Token_ExchangeAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Users.Token.ExchangeAsync((TeamsActivity)null!, "connection-name", "token"));
    }

    [Fact]
    public async Task Api_Users_Token_SignOutAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Users.Token.SignOutAsync((TeamsActivity)null!));
    }

    [Fact]
    public async Task Api_Users_Token_GetAadTokensAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Users.Token.GetAadTokensAsync((TeamsActivity)null!, "connection-name"));
    }

    [Fact]
    public async Task Api_Users_Token_GetStatusAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Users.Token.GetStatusAsync((TeamsActivity)null!));
    }

    [Fact]
    public async Task Api_Users_Token_GetSignInResourceAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Users.Token.GetSignInResourceAsync((TeamsActivity)null!, "connection-name"));
    }

    [Fact]
    public async Task Api_Conversations_Activities_SendHistoryAsync_ThrowsOnNullActivity()
    {
        var transcript = new Transcript { Activities = [] };
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Conversations.Activities.SendHistoryAsync((TeamsActivity)null!, transcript));
    }

    [Fact]
    public async Task Api_Conversations_Activities_GetMembersAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Conversations.Activities.GetMembersAsync((TeamsActivity)null!));
    }

    #region ConversationsApi Integration Tests

    [Fact]
    public async Task Api_Conversations_CreateAsync()
    {
        ConversationParameters parameters = new()
        {
            IsGroup = false,
            Members =
            [
                new()
                {
                    Id = await ResolveUserMriAsync(),
                }
            ],
            TenantId = Environment.GetEnvironmentVariable("AzureAd__TenantId") ?? throw new InvalidOperationException("AzureAd__TenantId environment variable not set")
        };

        CreateConversationResponse response = await _teamsBotApplication.Api.Conversations.CreateAsync(
            parameters,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response.Id);

        Console.WriteLine($"Created conversation via Api.Conversations.CreateAsync: {response.Id}");
    }

    #endregion

    #region ReactionsApi Integration Tests

    [Trait("Category", "needs-service-url")]
    [Fact]
    public async Task Api_Conversations_Reactions_AddAndDeleteAsync()
    {
        // First send an activity to react to
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Message for Reactions facade test at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            Conversation = new(_conversationId),
            From = _recipient
        };

        SendActivityResponse? sendResponse = await _teamsBotApplication.Api.Conversations.Activities.SendAsync(activity);
        Assert.NotNull(sendResponse?.Id);

        TeamsActivity teamsActivity = new()
        {
            ServiceUrl = _serviceUrl,
            Conversation = new TeamsConversation { Id = _conversationId },
            From = TeamsConversationAccount.FromConversationAccount(_recipient)
        };

        // Add a reaction
        await _teamsBotApplication.Api.Conversations.Reactions.AddAsync(
            teamsActivity,
            sendResponse.Id,
            "laugh",
            cancellationToken: CancellationToken.None);

        await Task.Delay(500);

        // Remove the reaction
        await _teamsBotApplication.Api.Conversations.Reactions.DeleteAsync(
            teamsActivity,
            sendResponse.Id,
            "laugh",
            cancellationToken: CancellationToken.None);

        Console.WriteLine($"Added and removed reaction via Api.Conversations.Reactions on activity {sendResponse.Id}");
    }

    [Fact]
    public async Task Api_Conversations_Reactions_AddAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Conversations.Reactions.AddAsync((TeamsActivity)null!, "activityId", "like"));
    }

    [Fact]
    public async Task Api_Conversations_Reactions_DeleteAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Conversations.Reactions.DeleteAsync((TeamsActivity)null!, "activityId", "like"));
    }

    [Fact]
    public async Task Api_Conversations_Reactions_AddAsync_ThrowsOnNullActivityId()
    {
        TeamsActivity teamsActivity = new() { ServiceUrl = _serviceUrl, Conversation = new TeamsConversation { Id = _conversationId } };
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Conversations.Reactions.AddAsync(teamsActivity, null!, "like"));
    }

    #endregion

    #region ActivitiesApi Missing Integration Tests

    [Trait("Category", "unsupported-api")]
    [Fact]
    public async Task Api_Conversations_Activities_SendHistoryAsync()
    {
        TeamsActivity teamsActivity = new()
        {
            ServiceUrl = _serviceUrl,
            Conversation = new TeamsConversation { Id = _conversationId },
            From = TeamsConversationAccount.FromConversationAccount(_recipient)
        };

        Transcript transcript = new()
        {
            Activities =
            [
                new()
                {
                    Type = ActivityType.Message,
                    Id = Guid.NewGuid().ToString(),
                    Properties = { { "text", "Historic message via facade" } },
                    ServiceUrl = _serviceUrl,
                    Conversation = new(_conversationId)
                }
            ]
        };

        SendConversationHistoryResponse response = await _teamsBotApplication.Api.Conversations.Activities.SendHistoryAsync(
            teamsActivity,
            transcript,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);
        Console.WriteLine($"Sent conversation history via facade: {response.Id}");
    }

    #endregion

    #region MembersApi Missing Integration Tests

    [Trait("Category", "unsupported-api")]
    [Fact]
    public async Task Api_Conversations_Members_DeleteAsync()
    {
        string memberToDelete = await ResolveUserMriAsync();

        TeamsActivity teamsActivity = new()
        {
            ServiceUrl = _serviceUrl,
            Conversation = new TeamsConversation { Id = _conversationId },
            From = TeamsConversationAccount.FromConversationAccount(_recipient)
        };

        await _teamsBotApplication.Api.Conversations.Members.DeleteAsync(
            teamsActivity,
            memberToDelete,
            cancellationToken: CancellationToken.None);

        Console.WriteLine($"Deleted member {memberToDelete} via Api.Conversations.Members.DeleteAsync");
    }

    #endregion

    #region MeetingsApi Missing Integration Tests

    [Trait("Category", "needs-meeting-context")]
    [Trait("Category", "needs-valid-domains")]
    [Fact]
    public async Task Api_Meetings_SendNotificationAsync()
    {
        string meetingId = Environment.GetEnvironmentVariable("TEST_MEETINGID") ?? throw new InvalidOperationException("TEST_MEETINGID environment variable not set");
        string participantId = (Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set")).Replace("29:", "");

        var notification = new TargetedMeetingNotification
        {
            Value = new TargetedMeetingNotificationValue
            {
                Recipients = [participantId],
                Surfaces =
                [
                    new MeetingNotificationSurface
                    {
                        Surface = "meetingStage",
                        ContentType = "task",
                        Content = new { title = "Test Notification", url = "https://klljrqz0-3978.usw2.devtunnels.ms/meetings" }
                    }
                ]
            }
        };

        MeetingNotificationResponse result = await _teamsBotApplication.Api.Meetings.SendNotificationAsync(
            meetingId,
            notification,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Console.WriteLine($"Meeting notification sent via facade to meeting {meetingId}");
    }

    #endregion

    #region BatchApi Missing Integration Tests

    [Trait("Category", "batch-isolation")]
    [Fact]
    public async Task Api_Batch_SendToUsersAsync()
    {
        string tenantId = Environment.GetEnvironmentVariable("TEST_TENANTID") ?? throw new InvalidOperationException("TEST_TENANTID environment variable not set");
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Batch message via facade at `{DateTime.UtcNow:s}`" } }
        };

        string operationId = await _teamsBotApplication.Api.Batch.SendToUsersAsync(
            activity,
            [new TeamMember(userId)],
            tenantId,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(operationId);
        Assert.NotEmpty(operationId);
        Console.WriteLine($"Batch SendToUsers via facade. Operation ID: {operationId}");
    }

    [Trait("Category", "batch-isolation")]
    [Fact]
    public async Task Api_Batch_SendToTenantAsync()
    {
        string tenantId = Environment.GetEnvironmentVariable("TEST_TENANTID") ?? throw new InvalidOperationException("TEST_TENANTID environment variable not set");

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Tenant-wide message via facade at `{DateTime.UtcNow:s}`" } }
        };

        string operationId = await _teamsBotApplication.Api.Batch.SendToTenantAsync(
            activity,
            tenantId,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(operationId);
        Assert.NotEmpty(operationId);
        Console.WriteLine($"Batch SendToTenant via facade. Operation ID: {operationId}");
    }

    [Trait("Category", "batch-isolation")]
    [Fact]
    public async Task Api_Batch_SendToTeamAsync()
    {
        string tenantId = Environment.GetEnvironmentVariable("TEST_TENANTID") ?? throw new InvalidOperationException("TEST_TENANTID environment variable not set");
        string teamId = Environment.GetEnvironmentVariable("TEST_TEAMID") ?? throw new InvalidOperationException("TEST_TEAMID environment variable not set");

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Team-wide message via facade at `{DateTime.UtcNow:s}`" } }
        };

        string operationId = await _teamsBotApplication.Api.Batch.SendToTeamAsync(
            activity,
            teamId,
            tenantId,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(operationId);
        Assert.NotEmpty(operationId);
        Console.WriteLine($"Batch SendToTeam via facade. Operation ID: {operationId}");
    }

    [Trait("Category", "batch-isolation")]
    [Fact]
    public async Task Api_Batch_SendToChannelsAsync()
    {
        string tenantId = Environment.GetEnvironmentVariable("TEST_TENANTID") ?? throw new InvalidOperationException("TEST_TENANTID environment variable not set");
        string channelId = Environment.GetEnvironmentVariable("TEST_CHANNELID") ?? throw new InvalidOperationException("TEST_CHANNELID environment variable not set");

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Channel batch message via facade at `{DateTime.UtcNow:s}`" } }
        };

        string operationId = await _teamsBotApplication.Api.Batch.SendToChannelsAsync(
            activity,
            [new TeamMember(channelId)],
            tenantId,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(operationId);
        Assert.NotEmpty(operationId);
        Console.WriteLine($"Batch SendToChannels via facade. Operation ID: {operationId}");
    }

    [Trait("Category", "batch-isolation")]
    [Fact]
    public async Task Api_Batch_GetFailedEntriesAsync()
    {
        string operationId = await SendBatchAndGetOperationIdAsync();

        BatchFailedEntriesResponse result = await _teamsBotApplication.Api.Batch.GetFailedEntriesAsync(
            operationId,
            _serviceUrl,
            agenticIdentity: _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Console.WriteLine($"Failed entries via facade for operation {operationId}");
    }

    [Trait("Category", "batch-isolation")]
    [Fact]
    public async Task Api_Batch_CancelAsync()
    {
        string operationId = await SendBatchAndGetOperationIdAsync();

        await _teamsBotApplication.Api.Batch.CancelAsync(
            operationId,
            _serviceUrl,
            _agenticIdentity,
            cancellationToken: CancellationToken.None);

        Console.WriteLine($"Operation {operationId} cancelled via facade");
    }

    #endregion

    #region UserTokenApi Integration Tests

    [Trait("Category", "needs-oauth-connection")]
    [Fact]
    public async Task Api_Users_Token_GetAsync()
    {
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");
        string connectionName = Environment.GetEnvironmentVariable("TEST_CONNECTION_NAME") ?? throw new InvalidOperationException("TEST_CONNECTION_NAME environment variable not set");

        var result = await _teamsBotApplication.Api.Users.Token.GetAsync(
            userId,
            connectionName,
            "msteams",
            cancellationToken: CancellationToken.None);

        Console.WriteLine($"GetAsync result: {(result != null ? "Token found" : "No token")}");
    }

    [Fact]
    public async Task Api_Users_Token_GetStatusAsync()
    {
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");

        var result = await _teamsBotApplication.Api.Users.Token.GetStatusAsync(
            userId,
            "msteams",
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Console.WriteLine($"Token status results: {result.Length}");
    }

    [Trait("Category", "needs-oauth-connection")]
    [Fact]
    public async Task Api_Users_Token_GetSignInResourceAsync()
    {
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");
        string connectionName = Environment.GetEnvironmentVariable("TEST_CONNECTION_NAME") ?? throw new InvalidOperationException("TEST_CONNECTION_NAME environment variable not set");

        var result = await _teamsBotApplication.Api.Users.Token.GetSignInResourceAsync(
            userId,
            connectionName,
            "msteams",
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.SignInLink);
        Console.WriteLine($"Sign-in resource via facade: {result.SignInLink}");
    }

    [Trait("Category", "needs-oauth-connection")]
    [Fact]
    public async Task Api_Users_Token_ExchangeAsync()
    {
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");
        string connectionName = Environment.GetEnvironmentVariable("TEST_CONNECTION_NAME") ?? throw new InvalidOperationException("TEST_CONNECTION_NAME environment variable not set");

        var result = await _teamsBotApplication.Api.Users.Token.ExchangeAsync(
            userId,
            connectionName,
            "msteams",
            "test-exchange-token",
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Console.WriteLine($"Exchange token via facade: Token={result.Token != null}");
    }

    [Fact]
    public async Task Api_Users_Token_SignOutAsync()
    {
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");

        await _teamsBotApplication.Api.Users.Token.SignOutAsync(
            userId,
            cancellationToken: CancellationToken.None);

        Console.WriteLine("SignOutAsync completed via facade");
    }

    [Trait("Category", "needs-oauth-connection")]
    [Fact]
    public async Task Api_Users_Token_GetAadTokensAsync()
    {
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");
        string connectionName = Environment.GetEnvironmentVariable("TEST_CONNECTION_NAME") ?? throw new InvalidOperationException("TEST_CONNECTION_NAME environment variable not set");

        var result = await _teamsBotApplication.Api.Users.Token.GetAadTokensAsync(
            userId,
            connectionName,
            "msteams",
            ["https://graph.microsoft.com"],
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Console.WriteLine($"AAD tokens via facade: {result.Count} entries");
    }

    #endregion
}
