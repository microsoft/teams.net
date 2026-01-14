// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core;
using Microsoft.Bot.Core.Hosting;
using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.BotApps;

namespace Microsoft.Bot.Core.Tests;

public class TeamsAPXClientTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly TeamsAPXClient _teamsClient;
    private readonly Uri _serviceUrl;

    public TeamsAPXClientTests()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddEnvironmentVariables();

        IConfiguration configuration = builder.Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddBotApplication<BotApplication>();
        _serviceProvider = services.BuildServiceProvider();
        _teamsClient = _serviceProvider.GetRequiredService<TeamsAPXClient>();
        _serviceUrl = new Uri(Environment.GetEnvironmentVariable("TEST_SERVICEURL") ?? "https://smba.trafficmanager.net/teams/");
    }

    #region Team Operations Tests

    [Fact]
    public async Task FetchChannelList()
    {
        string teamId = Environment.GetEnvironmentVariable("TEST_TEAMID") ?? throw new InvalidOperationException("TEST_TEAMID environment variable not set");

        ChannelList result = await _teamsClient.FetchChannelListAsync(
            teamId,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Conversations);
        Assert.NotEmpty(result.Conversations);

        Console.WriteLine($"Found {result.Conversations.Count} channels in team {teamId}:");
        foreach (var channel in result.Conversations)
        {
            Console.WriteLine($"  - Id: {channel.Id}, Name: {channel.Name}");
            Assert.NotNull(channel);
            Assert.NotNull(channel.Id);
        }
    }

    [Fact]
    public async Task FetchChannelList_FailsWithInvalidTeamId()
    {
        await Assert.ThrowsAsync<HttpRequestException>(()
            => _teamsClient.FetchChannelListAsync("invalid-team-id", _serviceUrl));
    }

    [Fact]
    public async Task FetchTeamDetails()
    {
        string teamId = Environment.GetEnvironmentVariable("TEST_TEAMID") ?? throw new InvalidOperationException("TEST_TEAMID environment variable not set");

        TeamDetails result = await _teamsClient.FetchTeamDetailsAsync(
            teamId,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Id);

        Console.WriteLine($"Team details for {teamId}:");
        Console.WriteLine($"  - Id: {result.Id}");
        Console.WriteLine($"  - Name: {result.Name}");
        Console.WriteLine($"  - AAD Group Id: {result.AadGroupId}");
        Console.WriteLine($"  - Channel Count: {result.ChannelCount}");
        Console.WriteLine($"  - Member Count: {result.MemberCount}");
        Console.WriteLine($"  - Type: {result.Type}");
    }

    [Fact]
    public async Task FetchTeamDetails_FailsWithInvalidTeamId()
    {
        await Assert.ThrowsAsync<HttpRequestException>(()
            => _teamsClient.FetchTeamDetailsAsync("invalid-team-id", _serviceUrl));
    }

    #endregion

    #region Meeting Operations Tests

    [Fact(Skip = "Requires active meeting context")]
    public async Task FetchMeetingInfo()
    {
        string meetingId = Environment.GetEnvironmentVariable("TEST_MEETINGID") ?? throw new InvalidOperationException("TEST_MEETINGID environment variable not set");

        MeetingInfo result = await _teamsClient.FetchMeetingInfoAsync(
            meetingId,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Id);

        Console.WriteLine($"Meeting info for {meetingId}:");
        Console.WriteLine($"  - Id: {result.Id}");
        if (result.Details != null)
        {
            Console.WriteLine($"  - Title: {result.Details.Title}");
            Console.WriteLine($"  - Type: {result.Details.Type}");
            Console.WriteLine($"  - Join URL: {result.Details.JoinUrl}");
            Console.WriteLine($"  - Scheduled Start: {result.Details.ScheduledStartTime}");
            Console.WriteLine($"  - Scheduled End: {result.Details.ScheduledEndTime}");
        }
        if (result.Organizer != null)
        {
            Console.WriteLine($"  - Organizer: {result.Organizer.Name} ({result.Organizer.Id})");
        }
    }

    [Fact]
    public async Task FetchMeetingInfo_FailsWithInvalidMeetingId()
    {
        await Assert.ThrowsAsync<HttpRequestException>(()
            => _teamsClient.FetchMeetingInfoAsync("invalid-meeting-id", _serviceUrl));
    }

    [Fact(Skip = "Requires active meeting context")]
    public async Task FetchParticipant()
    {
        string meetingId = Environment.GetEnvironmentVariable("TEST_MEETINGID") ?? throw new InvalidOperationException("TEST_MEETINGID environment variable not set");
        string participantId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");
        string tenantId = Environment.GetEnvironmentVariable("TENANT_ID") ?? throw new InvalidOperationException("TENANT_ID environment variable not set");

        MeetingParticipant result = await _teamsClient.FetchParticipantAsync(
            meetingId,
            participantId,
            tenantId,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);

        Console.WriteLine($"Participant info for {participantId} in meeting {meetingId}:");
        if (result.User != null)
        {
            Console.WriteLine($"  - User Id: {result.User.Id}");
            Console.WriteLine($"  - User Name: {result.User.Name}");
        }
        if (result.Meeting != null)
        {
            Console.WriteLine($"  - Role: {result.Meeting.Role}");
            Console.WriteLine($"  - In Meeting: {result.Meeting.InMeeting}");
        }
    }

    [Fact(Skip = "Requires active meeting context")]
    public async Task SendMeetingNotification()
    {
        string meetingId = Environment.GetEnvironmentVariable("TEST_MEETINGID") ?? throw new InvalidOperationException("TEST_MEETINGID environment variable not set");
        string participantId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");

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
                        Content = new { title = "Test Notification", url = "https://example.com" }
                    }
                ]
            }
        };

        MeetingNotificationResponse result = await _teamsClient.SendMeetingNotificationAsync(
            meetingId,
            notification,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);

        Console.WriteLine($"Meeting notification sent to meeting {meetingId}");
        if (result.RecipientsFailureInfo != null && result.RecipientsFailureInfo.Count > 0)
        {
            Console.WriteLine($"Failed recipients:");
            foreach (var failure in result.RecipientsFailureInfo)
            {
                Console.WriteLine($"  - {failure.RecipientMri}: {failure.ErrorCode} - {failure.FailureReason}");
            }
        }
    }

    #endregion

    #region Batch Message Operations Tests

    [Fact(Skip = "Batch operations require special permissions")]
    public async Task SendMessageToListOfUsers()
    {
        string tenantId = Environment.GetEnvironmentVariable("TENANT_ID") ?? throw new InvalidOperationException("TENANT_ID environment variable not set");
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Batch message from Automated tests at `{DateTime.UtcNow:s}`" } }
        };

        IList<TeamMember> members =
        [
            new TeamMember(userId)
        ];

        string operationId = await _teamsClient.SendMessageToListOfUsersAsync(
            activity,
            members,
            tenantId,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(operationId);
        Assert.NotEmpty(operationId);

        Console.WriteLine($"Batch message sent. Operation ID: {operationId}");
    }

    [Fact(Skip = "Batch operations require special permissions")]
    public async Task SendMessageToAllUsersInTenant()
    {
        string tenantId = Environment.GetEnvironmentVariable("TENANT_ID") ?? throw new InvalidOperationException("TENANT_ID environment variable not set");

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Tenant-wide message from Automated tests at `{DateTime.UtcNow:s}`" } }
        };

        string operationId = await _teamsClient.SendMessageToAllUsersInTenantAsync(
            activity,
            tenantId,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(operationId);
        Assert.NotEmpty(operationId);

        Console.WriteLine($"Tenant-wide message sent. Operation ID: {operationId}");
    }

    [Fact(Skip = "Batch operations require special permissions")]
    public async Task SendMessageToAllUsersInTeam()
    {
        string tenantId = Environment.GetEnvironmentVariable("TENANT_ID") ?? throw new InvalidOperationException("TENANT_ID environment variable not set");
        string teamId = Environment.GetEnvironmentVariable("TEST_TEAMID") ?? throw new InvalidOperationException("TEST_TEAMID environment variable not set");

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Team-wide message from Automated tests at `{DateTime.UtcNow:s}`" } }
        };

        string operationId = await _teamsClient.SendMessageToAllUsersInTeamAsync(
            activity,
            teamId,
            tenantId,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(operationId);
        Assert.NotEmpty(operationId);

        Console.WriteLine($"Team-wide message sent. Operation ID: {operationId}");
    }

    [Fact(Skip = "Batch operations require special permissions")]
    public async Task SendMessageToListOfChannels()
    {
        string tenantId = Environment.GetEnvironmentVariable("TENANT_ID") ?? throw new InvalidOperationException("TENANT_ID environment variable not set");
        string channelId = Environment.GetEnvironmentVariable("TEST_CHANNELID") ?? throw new InvalidOperationException("TEST_CHANNELID environment variable not set");

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Channel batch message from Automated tests at `{DateTime.UtcNow:s}`" } }
        };

        IList<TeamMember> channels =
        [
            new TeamMember(channelId)
        ];

        string operationId = await _teamsClient.SendMessageToListOfChannelsAsync(
            activity,
            channels,
            tenantId,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(operationId);
        Assert.NotEmpty(operationId);

        Console.WriteLine($"Channel batch message sent. Operation ID: {operationId}");
    }

    #endregion

    #region Batch Operation Management Tests

    [Fact(Skip = "Requires valid operation ID from batch operation")]
    public async Task GetOperationState()
    {
        string operationId = Environment.GetEnvironmentVariable("TEST_OPERATION_ID") ?? throw new InvalidOperationException("TEST_OPERATION_ID environment variable not set");

        BatchOperationState result = await _teamsClient.GetOperationStateAsync(
            operationId,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.State);

        Console.WriteLine($"Operation state for {operationId}:");
        Console.WriteLine($"  - State: {result.State}");
        Console.WriteLine($"  - Total Entries: {result.TotalEntriesCount}");
        if (result.StatusMap != null)
        {
            Console.WriteLine($"  - Success: {result.StatusMap.Success}");
            Console.WriteLine($"  - Failed: {result.StatusMap.Failed}");
            Console.WriteLine($"  - Throttled: {result.StatusMap.Throttled}");
            Console.WriteLine($"  - Pending: {result.StatusMap.Pending}");
        }
        if (result.RetryAfter != null)
        {
            Console.WriteLine($"  - Retry After: {result.RetryAfter}");
        }
    }

    [Fact]
    public async Task GetOperationState_FailsWithInvalidOperationId()
    {
        await Assert.ThrowsAsync<HttpRequestException>(()
            => _teamsClient.GetOperationStateAsync("invalid-operation-id", _serviceUrl));
    }

    [Fact(Skip = "Requires valid operation ID from batch operation")]
    public async Task GetPagedFailedEntries()
    {
        string operationId = Environment.GetEnvironmentVariable("TEST_OPERATION_ID") ?? throw new InvalidOperationException("TEST_OPERATION_ID environment variable not set");

        BatchFailedEntriesResponse result = await _teamsClient.GetPagedFailedEntriesAsync(
            operationId,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);

        Console.WriteLine($"Failed entries for operation {operationId}:");
        if (result.FailedEntries != null && result.FailedEntries.Count > 0)
        {
            foreach (var entry in result.FailedEntries)
            {
                Console.WriteLine($"  - Id: {entry.Id}, Error: {entry.Error}");
            }
        }
        else
        {
            Console.WriteLine("  No failed entries");
        }

        if (!string.IsNullOrWhiteSpace(result.ContinuationToken))
        {
            Console.WriteLine($"Continuation token: {result.ContinuationToken}");
        }
    }

    [Fact(Skip = "Requires valid operation ID from batch operation")]
    public async Task CancelOperation()
    {
        string operationId = Environment.GetEnvironmentVariable("TEST_OPERATION_ID") ?? throw new InvalidOperationException("TEST_OPERATION_ID environment variable not set");

        await _teamsClient.CancelOperationAsync(
            operationId,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        Console.WriteLine($"Operation {operationId} cancelled successfully");
    }

    #endregion

    #region Argument Validation Tests

    [Fact]
    public async Task FetchChannelList_ThrowsOnNullTeamId()
    {
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.FetchChannelListAsync(null!, _serviceUrl));
    }

    [Fact]
    public async Task FetchChannelList_ThrowsOnEmptyTeamId()
    {
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.FetchChannelListAsync("", _serviceUrl));
    }

    [Fact]
    public async Task FetchChannelList_ThrowsOnNullServiceUrl()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.FetchChannelListAsync("team-id", null!));
    }

    [Fact]
    public async Task FetchTeamDetails_ThrowsOnNullTeamId()
    {
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.FetchTeamDetailsAsync(null!, _serviceUrl));
    }

    [Fact]
    public async Task FetchMeetingInfo_ThrowsOnNullMeetingId()
    {
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.FetchMeetingInfoAsync(null!, _serviceUrl));
    }

    [Fact]
    public async Task FetchParticipant_ThrowsOnNullMeetingId()
    {
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.FetchParticipantAsync(null!, "participant", "tenant", _serviceUrl));
    }

    [Fact]
    public async Task FetchParticipant_ThrowsOnNullParticipantId()
    {
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.FetchParticipantAsync("meeting", null!, "tenant", _serviceUrl));
    }

    [Fact]
    public async Task FetchParticipant_ThrowsOnNullTenantId()
    {
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.FetchParticipantAsync("meeting", "participant", null!, _serviceUrl));
    }

    [Fact]
    public async Task SendMeetingNotification_ThrowsOnNullMeetingId()
    {
        var notification = new TargetedMeetingNotification();
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.SendMeetingNotificationAsync(null!, notification, _serviceUrl));
    }

    [Fact]
    public async Task SendMeetingNotification_ThrowsOnNullNotification()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.SendMeetingNotificationAsync("meeting", null!, _serviceUrl));
    }

    [Fact]
    public async Task SendMessageToListOfUsers_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.SendMessageToListOfUsersAsync(null!, [new TeamMember("id")], "tenant", _serviceUrl));
    }

    [Fact]
    public async Task SendMessageToListOfUsers_ThrowsOnNullMembers()
    {
        var activity = new CoreActivity { Type = ActivityType.Message };
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.SendMessageToListOfUsersAsync(activity, null!, "tenant", _serviceUrl));
    }

    [Fact]
    public async Task SendMessageToListOfUsers_ThrowsOnEmptyMembers()
    {
        var activity = new CoreActivity { Type = ActivityType.Message };
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.SendMessageToListOfUsersAsync(activity, [], "tenant", _serviceUrl));
    }

    [Fact]
    public async Task SendMessageToAllUsersInTenant_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.SendMessageToAllUsersInTenantAsync(null!, "tenant", _serviceUrl));
    }

    [Fact]
    public async Task SendMessageToAllUsersInTenant_ThrowsOnNullTenantId()
    {
        var activity = new CoreActivity { Type = ActivityType.Message };
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.SendMessageToAllUsersInTenantAsync(activity, null!, _serviceUrl));
    }

    [Fact]
    public async Task SendMessageToAllUsersInTeam_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.SendMessageToAllUsersInTeamAsync(null!, "team", "tenant", _serviceUrl));
    }

    [Fact]
    public async Task SendMessageToAllUsersInTeam_ThrowsOnNullTeamId()
    {
        var activity = new CoreActivity { Type = ActivityType.Message };
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.SendMessageToAllUsersInTeamAsync(activity, null!, "tenant", _serviceUrl));
    }

    [Fact]
    public async Task SendMessageToListOfChannels_ThrowsOnEmptyChannels()
    {
        var activity = new CoreActivity { Type = ActivityType.Message };
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.SendMessageToListOfChannelsAsync(activity, [], "tenant", _serviceUrl));
    }

    [Fact]
    public async Task GetOperationState_ThrowsOnNullOperationId()
    {
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.GetOperationStateAsync(null!, _serviceUrl));
    }

    [Fact]
    public async Task GetPagedFailedEntries_ThrowsOnNullOperationId()
    {
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.GetPagedFailedEntriesAsync(null!, _serviceUrl));
    }

    [Fact]
    public async Task CancelOperation_ThrowsOnNullOperationId()
    {
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.CancelOperationAsync(null!, _serviceUrl));
    }

    #endregion
}
