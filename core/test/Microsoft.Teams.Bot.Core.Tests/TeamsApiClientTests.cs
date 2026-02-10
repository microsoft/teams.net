// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;
using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Teams.Bot.Core.Tests;
using Xunit.Abstractions;

namespace Microsoft.Bot.Core.Tests;

public class TeamsApiClientTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly TeamsApiClient _teamsClient;
    private readonly Uri _serviceUrl;
    private readonly ITestOutputHelper testOutput;

    public TeamsApiClientTests(ITestOutputHelper outputHelper)
    {
        testOutput = outputHelper;
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddEnvironmentVariables();

        IConfiguration configuration = builder.Build();

        ServiceCollection services = new();
        services.AddLogging((builder) => {
            builder.AddXUnit(outputHelper);
            builder.AddFilter("System.Net", LogLevel.Warning);
            builder.AddFilter("Microsoft.Identity", LogLevel.Warning);
            builder.AddFilter("Microsoft.Teams", LogLevel.Information);
        });
        services.AddSingleton(configuration);
        services.AddTeamsBotApplication();
        _serviceProvider = services.BuildServiceProvider();
        _teamsClient = _serviceProvider.GetRequiredService<TeamsApiClient>();
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
            AgenticIdentitiyFromEnv.GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Channels);
        Assert.NotEmpty(result.Channels);

        testOutput.WriteLine($"Found {result.Channels.Count} channels in team {teamId}:");
        foreach (var channel in result.Channels)
        {
            testOutput.WriteLine($"  - Id: {channel.Id}, Name: {channel.Name}");
            Assert.NotNull(channel);
            Assert.NotNull(channel.Id);
        }
    }

    [Fact]
    public async Task FetchChannelList_FailsWithInvalidTeamId()
    {
        await Assert.ThrowsAsync<HttpRequestException>(()
            => _teamsClient.FetchChannelListAsync("invalid-team-id", _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task FetchTeamDetails()
    {
        string teamId = Environment.GetEnvironmentVariable("TEST_TEAMID") ?? throw new InvalidOperationException("TEST_TEAMID environment variable not set");

        TeamDetails result = await _teamsClient.FetchTeamDetailsAsync(
            teamId,
            _serviceUrl,
            AgenticIdentitiyFromEnv.GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Id);

        testOutput.WriteLine($"Team details for {teamId}:");
        testOutput.WriteLine($"  - Id: {result.Id}");
        testOutput.WriteLine($"  - Name: {result.Name}");
        testOutput.WriteLine($"  - AAD Group Id: {result.AadGroupId}");
        testOutput.WriteLine($"  - Channel Count: {result.ChannelCount}");
        testOutput.WriteLine($"  - Member Count: {result.MemberCount}");
        testOutput.WriteLine($"  - Type: {result.Type}");
    }

    [Fact]
    public async Task FetchTeamDetails_FailsWithInvalidTeamId()
    {
        await Assert.ThrowsAsync<HttpRequestException>(()
            => _teamsClient.FetchTeamDetailsAsync("invalid-team-id", _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    #endregion

    #region Meeting Operations Tests

    [Fact]
    public async Task FetchMeetingInfo()
    {
        string meetingId = Environment.GetEnvironmentVariable("TEST_MEETINGID") ?? throw new InvalidOperationException("TEST_MEETINGID environment variable not set");

        MeetingInfo result = await _teamsClient.FetchMeetingInfoAsync(
            meetingId,
            _serviceUrl,
            AgenticIdentitiyFromEnv.GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        //Assert.NotNull(result.Id);

        testOutput.WriteLine($"Meeting info for {meetingId}:");
        
        if (result.Details != null)
        {
            testOutput.WriteLine($"  - Title: {result.Details.Title}");
            testOutput.WriteLine($"  - Type: {result.Details.Type}");
            testOutput.WriteLine($"  - Join URL: {result.Details.JoinUrl}");
            testOutput.WriteLine($"  - Scheduled Start: {result.Details.ScheduledStartTime}");
            testOutput.WriteLine($"  - Scheduled End: {result.Details.ScheduledEndTime}");
        }
        if (result.Organizer != null)
        {
            testOutput.WriteLine($"  - Organizer: {result.Organizer.Name} ({result.Organizer.Id})");
        }
    }

    [Fact]
    public async Task FetchMeetingInfo_FailsWithInvalidMeetingId()
    {
        await Assert.ThrowsAsync<HttpRequestException>(()
            => _teamsClient.FetchMeetingInfoAsync("invalid-meeting-id", _serviceUrl));
    }

    [Fact]
    public async Task FetchParticipant()
    {
        string meetingId = Environment.GetEnvironmentVariable("TEST_MEETINGID") ?? throw new InvalidOperationException("TEST_MEETINGID environment variable not set");
        string participantId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");
        string tenantId = Environment.GetEnvironmentVariable("TEST_TENANTID") ?? throw new InvalidOperationException("TEST_TENANTID environment variable not set");

        MeetingParticipant result = await _teamsClient.FetchParticipantAsync(
            meetingId,
            participantId,
            tenantId,
            _serviceUrl,
            AgenticIdentitiyFromEnv.GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);

        testOutput.WriteLine($"Participant info for {participantId} in meeting {meetingId}:");
        if (result.User != null)
        {
            testOutput.WriteLine($"  - User Id: {result.User.Id}");
            testOutput.WriteLine($"  - User Name: {result.User.Name}");
        }
        if (result.Meeting != null)
        {
            testOutput.WriteLine($"  - Role: {result.Meeting.Role}");
            testOutput.WriteLine($"  - In Meeting: {result.Meeting.InMeeting}");
        }
    }

    [Fact]
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
            AgenticIdentitiyFromEnv.GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);

        testOutput.WriteLine($"Meeting notification sent to meeting {meetingId}");
        if (result.RecipientsFailureInfo != null && result.RecipientsFailureInfo.Count > 0)
        {
            testOutput.WriteLine($"Failed recipients:");
            foreach (var failure in result.RecipientsFailureInfo)
            {
                testOutput.WriteLine($"  - {failure.RecipientMri}: {failure.ErrorCode} - {failure.FailureReason}");
            }
        }
    }

    #endregion

    #region Batch Message Operations Tests

    [Fact]
    public async Task SendMessageToListOfUsers()
    {
        string tenantId = Environment.GetEnvironmentVariable("TEST_TENANTID") ?? throw new InvalidOperationException("TEST_TENANTID environment variable not set");
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Batch message from Automated tests at `{DateTime.UtcNow:s}`" } }
        };

        IList<TeamMember> members =
        [
            new TeamMember(userId),
            new TeamMember(userId),
            new TeamMember(userId),
            new TeamMember(userId),
            new TeamMember(userId),
            new TeamMember(userId)
        ];

        string operationId = await _teamsClient.SendMessageToListOfUsersAsync(
            activity,
            members,
            tenantId,
            _serviceUrl,
            AgenticIdentitiyFromEnv.GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(operationId);
        Assert.NotEmpty(operationId);

        testOutput.WriteLine($"Batch message sent. Operation ID: {operationId}");
    }

    [Fact]
    public async Task SendMessageToAllUsersInTenant()
    {
        string tenantId = Environment.GetEnvironmentVariable("TEST_TENANTID") ?? throw new InvalidOperationException("TEST_TENANTID environment variable not set");

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Properties = { { "text", $"Tenant-wide message from Automated tests at `{DateTime.UtcNow:s}`" } }
        };

        string operationId = await _teamsClient.SendMessageToAllUsersInTenantAsync(
            activity,
            tenantId,
            _serviceUrl,
            AgenticIdentitiyFromEnv.GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(operationId);
        Assert.NotEmpty(operationId);

        testOutput.WriteLine($"Tenant-wide message sent. Operation ID: {operationId}");
    }

    [Fact]
    public async Task SendMessageToAllUsersInTeam()
    {
        string tenantId = Environment.GetEnvironmentVariable("TEST_TENANTID") ?? throw new InvalidOperationException("TEST_TENANTID environment variable not set");
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
            AgenticIdentitiyFromEnv.GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(operationId);
        Assert.NotEmpty(operationId);

        testOutput.WriteLine($"Team-wide message sent. Operation ID: {operationId}");
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
            AgenticIdentitiyFromEnv.GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(operationId);
        Assert.NotEmpty(operationId);

        testOutput.WriteLine($"Channel batch message sent. Operation ID: {operationId}");
    }

    #endregion

    #region Batch Operation Management Tests

    [Fact]
    public async Task GetOperationState()
    {
        string operationId = "amer_9d3424a5-6ce6-477f-934d-59e8ea5f7f27"; // Environment.GetEnvironmentVariable("TEST_OPERATION_ID") ?? throw new InvalidOperationException("TEST_OPERATION_ID environment variable not set");

        BatchOperationState result = await _teamsClient.GetOperationStateAsync(
            operationId,
            _serviceUrl,
            AgenticIdentitiyFromEnv.GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.State);

        testOutput.WriteLine($"Operation state for {operationId}:");
        testOutput.WriteLine($"  - State: {result.State}");
        testOutput.WriteLine($"  - Total Entries: {result.TotalEntriesCount}");
        if (result.StatusMap != null)
        {
            testOutput.WriteLine($"  - Success: {result.StatusMap.Success}");
            testOutput.WriteLine($"  - Failed: {result.StatusMap.Failed}");
            testOutput.WriteLine($"  - Throttled: {result.StatusMap.Throttled}");
            testOutput.WriteLine($"  - Pending: {result.StatusMap.Pending}");
        }
        if (result.RetryAfter != null)
        {
            testOutput.WriteLine($"  - Retry After: {result.RetryAfter}");
        }
    }

    [Fact]
    public async Task GetOperationState_FailsWithInvalidOperationId()
    {
        await Assert.ThrowsAsync<HttpRequestException>(()
            => _teamsClient.GetOperationStateAsync("invalid-operation-id", _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact(Skip = "Requires valid operation ID from batch operation")]
    public async Task GetPagedFailedEntries()
    {
        string operationId = Environment.GetEnvironmentVariable("TEST_OPERATION_ID") ?? throw new InvalidOperationException("TEST_OPERATION_ID environment variable not set");

        BatchFailedEntriesResponse result = await _teamsClient.GetPagedFailedEntriesAsync(
            operationId,
            _serviceUrl,
            null,
            AgenticIdentitiyFromEnv.GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);

        testOutput.WriteLine($"Failed entries for operation {operationId}:");
        if (result.FailedEntries != null && result.FailedEntries.Count > 0)
        {
            foreach (var entry in result.FailedEntries)
            {
                testOutput.WriteLine($"  - Id: {entry.Id}, Error: {entry.Error}");
            }
        }
        else
        {
            testOutput.WriteLine("  No failed entries");
        }

        if (!string.IsNullOrWhiteSpace(result.ContinuationToken))
        {
            testOutput.WriteLine($"Continuation token: {result.ContinuationToken}");
        }
    }

    [Fact(Skip = "Requires valid operation ID from batch operation")]
    public async Task CancelOperation()
    {
        string operationId = Environment.GetEnvironmentVariable("TEST_OPERATION_ID") ?? throw new InvalidOperationException("TEST_OPERATION_ID environment variable not set");

        await _teamsClient.CancelOperationAsync(
            operationId,
            _serviceUrl,
            AgenticIdentitiyFromEnv.GetAgenticIdentity(),
            cancellationToken: CancellationToken.None);

        testOutput.WriteLine($"Operation {operationId} cancelled successfully");
    }

    #endregion

    #region Argument Validation Tests

    [Fact]
    public async Task FetchChannelList_ThrowsOnNullTeamId()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.FetchChannelListAsync(null!, _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task FetchChannelList_ThrowsOnEmptyTeamId()
    {
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.FetchChannelListAsync("", _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task FetchChannelList_ThrowsOnNullServiceUrl()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.FetchChannelListAsync("team-id", null!, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task FetchTeamDetails_ThrowsOnNullTeamId()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.FetchTeamDetailsAsync(null!, _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task FetchMeetingInfo_ThrowsOnNullMeetingId()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.FetchMeetingInfoAsync(null!, _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task FetchParticipant_ThrowsOnNullMeetingId()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.FetchParticipantAsync(null!, "participant", "tenant", _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task FetchParticipant_ThrowsOnNullParticipantId()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.FetchParticipantAsync("meeting", null!, "tenant", _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task FetchParticipant_ThrowsOnNullTenantId()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.FetchParticipantAsync("meeting", "participant", null!, _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task SendMeetingNotification_ThrowsOnNullMeetingId()
    {
        var notification = new TargetedMeetingNotification();
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.SendMeetingNotificationAsync(null!, notification, _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task SendMeetingNotification_ThrowsOnNullNotification()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.SendMeetingNotificationAsync("meeting", null!, _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task SendMessageToListOfUsers_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.SendMessageToListOfUsersAsync(null!, [new TeamMember("id")], "tenant", _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task SendMessageToListOfUsers_ThrowsOnNullMembers()
    {
        var activity = new CoreActivity { Type = ActivityType.Message };
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.SendMessageToListOfUsersAsync(activity, null!, "tenant", _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task SendMessageToListOfUsers_ThrowsOnEmptyMembers()
    {
        var activity = new CoreActivity { Type = ActivityType.Message };
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.SendMessageToListOfUsersAsync(activity, [], "tenant", _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task SendMessageToAllUsersInTenant_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.SendMessageToAllUsersInTenantAsync(null!, "tenant", _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task SendMessageToAllUsersInTenant_ThrowsOnNullTenantId()
    {
        var activity = new CoreActivity { Type = ActivityType.Message };
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.SendMessageToAllUsersInTenantAsync(activity, null!, _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task SendMessageToAllUsersInTeam_ThrowsOnNullActivity()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.SendMessageToAllUsersInTeamAsync(null!, "team", "tenant", _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task SendMessageToAllUsersInTeam_ThrowsOnNullTeamId()
    {
        var activity = new CoreActivity { Type = ActivityType.Message };
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.SendMessageToAllUsersInTeamAsync(activity, null!, "tenant", _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task SendMessageToListOfChannels_ThrowsOnEmptyChannels()
    {
        var activity = new CoreActivity { Type = ActivityType.Message };
        await Assert.ThrowsAsync<ArgumentException>(()
            => _teamsClient.SendMessageToListOfChannelsAsync(activity, [], "tenant", _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task GetOperationState_ThrowsOnNullOperationId()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.GetOperationStateAsync(null!, _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task GetPagedFailedEntries_ThrowsOnNullOperationId()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.GetPagedFailedEntriesAsync(null!, _serviceUrl, null, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    [Fact]
    public async Task CancelOperation_ThrowsOnNullOperationId()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(()
            => _teamsClient.CancelOperationAsync(null!, _serviceUrl, AgenticIdentitiyFromEnv.GetAgenticIdentity()));
    }

    #endregion
}
