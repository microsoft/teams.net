// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;
using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Api;
using Microsoft.Teams.Bot.Apps.Schema;

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

    public TeamsApiFacadeTests()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddEnvironmentVariables();

        IConfiguration configuration = builder.Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddHttpContextAccessor();
        services.AddTeamsBotApplication();
        _serviceProvider = services.BuildServiceProvider();
        _teamsBotApplication = _serviceProvider.GetRequiredService<TeamsBotApplication>();
        _serviceUrl = new Uri(Environment.GetEnvironmentVariable("TEST_SERVICEURL") ?? "https://smba.trafficmanager.net/teams/");
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
            From = new TeamsConversationAccount { Id = "test-user" }
        };

        TeamDetails result = await _teamsBotApplication.Api.Teams.GetByIdAsync(
            teamId,
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
            Conversation = new()
            {
                Id = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set")
            }
        };

        SendActivityResponse res = await _teamsBotApplication.Api.Conversations.Activities.SendAsync(
            activity,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(res);
        Assert.NotNull(res.Id);

        Console.WriteLine($"Sent activity via Api.Conversations.Activities.SendAsync: {res.Id}");
    }

    [Fact]
    public async Task Api_Conversations_Activities_UpdateAsync()
    {
        string conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");

        // First send an activity
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Original message via Api at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            Conversation = new() { Id = conversationId }
        };

        SendActivityResponse sendResponse = await _teamsBotApplication.Api.Conversations.Activities.SendAsync(activity);
        Assert.NotNull(sendResponse?.Id);

        // Now update the activity
        CoreActivity updatedActivity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Updated message via Api.Conversations.Activities.UpdateAsync at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
        };

        UpdateActivityResponse updateResponse = await _teamsBotApplication.Api.Conversations.Activities.UpdateAsync(
            conversationId,
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
        string conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");

        // First send an activity
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Message to delete via Api at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            Conversation = new() { Id = conversationId }
        };

        SendActivityResponse sendResponse = await _teamsBotApplication.Api.Conversations.Activities.SendAsync(activity);
        Assert.NotNull(sendResponse?.Id);

        // Wait a bit before deleting
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Now delete the activity
        await _teamsBotApplication.Api.Conversations.Activities.DeleteAsync(
            conversationId,
            sendResponse.Id,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Console.WriteLine($"Deleted activity via Api.Conversations.Activities.DeleteAsync: {sendResponse.Id}");
    }

    [Fact]
    public async Task Api_Conversations_Activities_GetMembersAsync()
    {
        string conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");

        // First send an activity
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Message for GetMembersAsync test at `{DateTime.UtcNow:s}`" } },
            ServiceUrl = _serviceUrl,
            Conversation = new() { Id = conversationId }
        };

        SendActivityResponse sendResponse = await _teamsBotApplication.Api.Conversations.Activities.SendAsync(activity);
        Assert.NotNull(sendResponse?.Id);

        // Now get activity members
        IList<ConversationAccount> members = await _teamsBotApplication.Api.Conversations.Activities.GetMembersAsync(
            conversationId,
            sendResponse.Id,
            _serviceUrl,
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
        string conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");

        IList<ConversationAccount> members = await _teamsBotApplication.Api.Conversations.Members.GetAllAsync(
            conversationId,
            _serviceUrl,
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
        string conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");

        TeamsActivity activity = new()
        {
            ServiceUrl = _serviceUrl,
            Conversation = new TeamsConversation { Id = conversationId },
            From = new TeamsConversationAccount { Id = "test-user" }
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
        string conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");

        ConversationAccount member = await _teamsBotApplication.Api.Conversations.Members.GetByIdAsync(
            conversationId,
            userId,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(member);
        Assert.NotNull(member.Id);

        Console.WriteLine($"Found member via Api.Conversations.Members.GetByIdAsync:");
        Console.WriteLine($"  - Id: {member.Id}, Name: {member.Name}");
    }

    [Fact]
    public async Task Api_Conversations_Members_GetPagedAsync()
    {
        string conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");

        PagedMembersResult result = await _teamsBotApplication.Api.Conversations.Members.GetPagedAsync(
            conversationId,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Members);
        Assert.NotEmpty(result.Members);

        Console.WriteLine($"Found {result.Members.Count} members via Api.Conversations.Members.GetPagedAsync");
    }

    [Fact]
    public async Task Api_Meetings_GetByIdAsync()
    {
        string meetingId = Environment.GetEnvironmentVariable("TEST_MEETINGID") ?? throw new InvalidOperationException("TEST_MEETINGID environment variable not set");

        MeetingInfo result = await _teamsBotApplication.Api.Meetings.GetByIdAsync(
            meetingId,
            _serviceUrl,
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
            => _teamsBotApplication.Api.Batch.GetStateAsync("invalid-operation-id", _serviceUrl));
    }

    [Fact]
    public async Task Api_Teams_GetByIdAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Teams.GetByIdAsync("team-id", (TeamsActivity)null!));
    }

    [Fact]
    public async Task Api_Teams_GetChannelsAsync_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsBotApplication.Api.Teams.GetChannelsAsync("team-id", (TeamsActivity)null!));
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
