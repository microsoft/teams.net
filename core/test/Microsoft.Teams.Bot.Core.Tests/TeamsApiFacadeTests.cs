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

        SendActivityResponse res = await _teamsBotApplication.Api.Conversations.Activities.SendAsync(
            activity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(res);
        Assert.NotNull(res.Id);

        Console.WriteLine($"Sent activity via Api.Conversations.Activities.SendAsync: {res.Id}");
    }


    [Fact]
    public async Task Api_Conversations_Activities_Send_Update_DeleteTMAsync()
    {
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");

        CoreActivity activity = CoreActivity.CreateBuilder()
            .WithType(ActivityType.Message)
            .WithServiceUrl(_serviceUrl)
            .WithConversation(new(_conversationId))
            .WithFrom(_recipient)
            .WithRecipient(new ConversationAccount() { Id = userId }, isTargeted: true)
            .WithProperty("text", $"TM Message via Api.Conversations.Activities.SendAsync at `{DateTime.UtcNow:s}`")
            .Build();

        SendActivityResponse res = await _teamsBotApplication.Api.Conversations.Activities.SendAsync(
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

        SendActivityResponse sendResponse = await _teamsBotApplication.Api.Conversations.Activities.SendAsync(activity);
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

        SendActivityResponse sendResponse = await _teamsBotApplication.Api.Conversations.Activities.SendAsync(activity);
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

        SendActivityResponse sendResponse = await _teamsBotApplication.Api.Conversations.Activities.SendAsync(activity);
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
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");

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

    [Fact(Skip = "GetByIdAsync is not working with agentic identity")]
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

    [Fact]
    public async Task Api_Meetings_GetParticipantAsync()
    {
        string meetingId = Environment.GetEnvironmentVariable("TEST_MEETINGID") ?? throw new InvalidOperationException("TEST_MEETINGID environment variable not set");
        string participantId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");
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
}
