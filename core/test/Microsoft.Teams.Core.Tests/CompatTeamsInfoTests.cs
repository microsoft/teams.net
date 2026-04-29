// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.BotBuilder;
using Xunit.Abstractions;

namespace Microsoft.Bot.Core.Tests
{
    /// <summary>
    /// Integration tests for TeamsApiClient static methods.
    /// These tests verify that the compatibility layer correctly adapts
    /// Bot Framework TeamsInfo API to Teams Bot Core SDK.
    /// </summary>
    public class TeamsApiClientTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly string _serviceUrl = "https://smba.trafficmanager.net/amer/";
        private readonly string _userId;
        private readonly string _conversationId;
        private readonly string _teamId;
        private readonly string _channelId;
        private readonly string _meetingId;
        private readonly string _tenantId;
        private readonly string _agenticAppBlueprintId;
        private readonly string? _agenticAppId;
        private readonly string? _agenticUserId;

        public TeamsApiClientTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            // These tests require environment variables for live integration testing
            _userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? "29:test-user-id";
            _conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? "19:test-conversation-id";
            _teamId = Environment.GetEnvironmentVariable("TEST_TEAMID") ?? "19:test-team-id";
            _channelId = Environment.GetEnvironmentVariable("TEST_CHANNELID") ?? "19:test-channel-id";
            _meetingId = Environment.GetEnvironmentVariable("TEST_MEETINGID") ?? "test-meeting-id";
            _tenantId = Environment.GetEnvironmentVariable("TEST_TENANTID") ?? "test-tenant-id";

            _agenticAppBlueprintId = Environment.GetEnvironmentVariable("AzureAd__ClientId") ?? throw new InvalidOperationException("AzureAd__ClientId environment variable not set");
            _agenticAppId = Environment.GetEnvironmentVariable("TEST_AGENTIC_APPID");// ?? throw new InvalidOperationException("TEST_AGENTIC_APPID environment variable not set");
            _agenticUserId = Environment.GetEnvironmentVariable("TEST_AGENTIC_USERID");
        }

        [Fact]
        public async Task GetMemberAsync_WithValidUserId_ReturnsMember()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    TeamsChannelAccount member = await TeamsApiClient.GetMemberAsync(
                        turnContext,
                        _userId,
                        cancellationToken);

                    Assert.NotNull(member);
                    Assert.Equal(_userId, member.Id);
                },
                CancellationToken.None);
        }

        [Fact]
        public async Task GetMembersAsync_ReturnsMembers()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    var members = await TeamsApiClient.GetMembersAsync(turnContext, cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete

                    Assert.NotNull(members);
                    Assert.NotEmpty(members);
                },
                CancellationToken.None);
        }

        [Fact]
        public async Task GetPagedMembersAsync_ReturnsPagedResult()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var result = await TeamsApiClient.GetPagedMembersAsync(
                        turnContext,
                        pageSize: 10,
                        cancellationToken: cancellationToken);

                    Assert.NotNull(result);
                    Assert.NotNull(result.Members);
                    Assert.True(result.Members.Count > 0);

                    var firstMember = result.Members[0];
                    Assert.NotNull(firstMember.Id);
                },
                CancellationToken.None);
        }

        [Fact]
        public async Task GetTeamMemberAsync_WithValidUserId_ReturnsMember()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var member = await TeamsApiClient.GetTeamMemberAsync(
                        turnContext,
                        _userId,
                        _teamId,
                        cancellationToken);

                    Assert.NotNull(member);
                    Assert.Equal(_userId, member.Id);
                },
                CancellationToken.None);
        }

        [Fact]
        public async Task GetTeamMembersAsync_ReturnsTeamMembers()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    var members = await TeamsApiClient.GetTeamMembersAsync(
                        turnContext,
                        _teamId,
                        cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete

                    Assert.NotNull(members);
                    Assert.NotEmpty(members);
                },
                CancellationToken.None);
        }

        [Fact]
        public async Task GetPagedTeamMembersAsync_ReturnsPagedResult()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var result = await TeamsApiClient.GetPagedTeamMembersAsync(
                        turnContext,
                        _teamId,
                        pageSize: 5,
                        cancellationToken: cancellationToken);

                    Assert.NotNull(result);
                    Assert.NotNull(result.Members);
                },
                CancellationToken.None);
        }

        [Fact(Skip = "permissions needed")]
        public async Task GetMeetingInfoAsync_WithMeetingId_ReturnsMeetingInfo()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var meetingInfo = await TeamsApiClient.GetMeetingInfoAsync(
                        turnContext,
                        _meetingId,
                        cancellationToken);

                    Assert.NotNull(meetingInfo);
                    Assert.NotNull(meetingInfo.Details);
                },
                CancellationToken.None);
        }

        [Fact]
        public async Task GetMeetingParticipantAsync_WithParticipantId_ReturnsParticipant()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var participant = await TeamsApiClient.GetMeetingParticipantAsync(
                        turnContext,
                        _meetingId,
                        _userId,
                        _tenantId,
                        cancellationToken);

                    Assert.NotNull(participant);
                    Assert.NotNull(participant.User);
                },
                CancellationToken.None);
        }

        [Fact(Skip = "Permissions")]
        public async Task SendMeetingNotificationAsync_SendsNotification()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    // Create a simple targeted meeting notification
                    // Note: In real scenarios, you would construct the proper notification object
                    // with surfaces and content according to the Teams schema
                    var notification = new TargetedMeetingNotification
                    {
                        Value = new TargetedMeetingNotificationValue
                        {
                            Recipients = new List<string> { _userId },
                            Surfaces = new List<Surface>
                            {
                                new MeetingStageSurface<TaskModuleContinueResponse>()
                                {
                                    ContentType = ContentType.Task,
                                    Content = new TaskModuleContinueResponse
                                    {
                                        Value = new TaskModuleTaskInfo
                                        {
                                            Title = "Test Notification",
                                            Url = "https://www.example.com",
                                            Height = 200,
                                            Width = 400
                                        }
                                    }
                                }
                            }
                        }
                    };

                    var response = await TeamsApiClient.SendMeetingNotificationAsync(
                        turnContext,
                        notification,
                        _meetingId,
                        cancellationToken);

                    Assert.NotNull(response);
                },
                CancellationToken.None);
        }

        [Fact]
        public async Task GetTeamDetailsAsync_WithTeamId_ReturnsTeamDetails()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var teamDetails = await TeamsApiClient.GetTeamDetailsAsync(
                        turnContext,
                        _teamId,
                        cancellationToken);

                    Assert.NotNull(teamDetails);
                    Assert.NotNull(teamDetails.Id);
                    Assert.NotNull(teamDetails.Name);
                },
                CancellationToken.None);
        }

        [Fact]
        public async Task GetTeamChannelsAsync_WithTeamId_ReturnsChannels()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var channels = await TeamsApiClient.GetTeamChannelsAsync(
                        turnContext,
                        _teamId,
                        cancellationToken);

                    Assert.NotNull(channels);
                    Assert.NotEmpty(channels.Conversations);

                    var firstChannel = channels.Conversations[0];
                    Assert.NotNull(firstChannel.Id);
                    Assert.NotNull(firstChannel.Name);
                },
                CancellationToken.None);
        }

        [Fact(Skip = "Investigate with activity to send")]
        public async Task SendMessageToListOfUsersAsync_ReturnsOperationId()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var activity = new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = "Test message"
                    };
                    var members = new List<TeamMember>
                    {
                        new TeamMember(_channelId),
                        new TeamMember("1"),
                        new TeamMember("2"),
                        new TeamMember("4"),
                        new TeamMember("5"),
                        new TeamMember("6")

                    };

                    var operationId = await TeamsApiClient.SendMessageToListOfUsersAsync(
                        turnContext,
                        activity,
                        members,
                        _tenantId,
                        cancellationToken);

                    Assert.NotNull(operationId);
                    Assert.NotEmpty(operationId);
                },
                CancellationToken.None);
        }

        [Fact(Skip = "Investigate with activity to send")]
        public async Task SendMessageToListOfChannelsAsync_ReturnsOperationId()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var activity = new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = "Test message"
                    };
                    var channels = new List<TeamMember>
                    {
                        new TeamMember(_channelId),
                        new TeamMember("1"),
                        new TeamMember("2"),
                        new TeamMember("4"),
                        new TeamMember("5"),
                        new TeamMember("6")
                    };

                    var operationId = await TeamsApiClient.SendMessageToListOfChannelsAsync(
                        turnContext,
                        activity,
                        channels,
                        _tenantId,
                        cancellationToken);

                    Assert.NotNull(operationId);
                    Assert.NotEmpty(operationId);
                },
                CancellationToken.None);
        }

        [Fact(Skip ="Investigate with activity to send")]
        public async Task SendMessageToAllUsersInTeamAsync_ReturnsOperationId()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var activity = new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = "Test message to team"
                    };

                    var operationId = await TeamsApiClient.SendMessageToAllUsersInTeamAsync(
                        turnContext,
                        activity,
                        _teamId,
                        _tenantId,
                        cancellationToken);

                    Assert.NotNull(operationId);
                    Assert.NotEmpty(operationId);
                },
                CancellationToken.None);
        }

        [Fact(Skip = "Investigate with activity to send")]
        public async Task SendMessageToAllUsersInTenantAsync_ReturnsOperationId()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var activity = new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = "Test message to tenant"
                    };

                    var operationId = await TeamsApiClient.SendMessageToAllUsersInTenantAsync(
                        turnContext,
                        activity,
                        _tenantId,
                        cancellationToken);

                    Assert.NotNull(operationId);
                    Assert.NotEmpty(operationId);
                },
                CancellationToken.None);
        }

        [Fact(Skip = "Not implemented")]
        public async Task SendMessageToTeamsChannelAsync_CreatesConversationAndSendsMessage()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var activity = new Activity
                    {
                        Type = ActivityTypes.Message,
                        Text = "Test message to channel"
                    };
                    var botAppId = Environment.GetEnvironmentVariable("AzureAd__ClientId") ?? string.Empty;

                    var result = await TeamsApiClient.SendMessageToTeamsChannelAsync(
                        turnContext,
                        activity,
                        _channelId,
                        botAppId,
                        cancellationToken);

                    Assert.NotNull(result);
                    Assert.NotNull(result.Item1); // ConversationReference
                    Assert.NotNull(result.Item2); // ActivityId
                },
                CancellationToken.None);
        }

        [Fact(Skip = "Internal Server Error")]
        public async Task GetOperationStateAsync_WithOperationId_ReturnsState()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);
            var operationId = "amer_9e0e3ba8-c562-440f-ba9d-10603ee31837";

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var state = await TeamsApiClient.GetOperationStateAsync(
                        turnContext,
                        operationId,
                        cancellationToken);

                    Assert.NotNull(state);
                    Assert.NotNull(state.State);
                },
                CancellationToken.None);
        }

        [Fact(Skip = "Internal Server Error")]
        public async Task GetPagedFailedEntriesAsync_WithOperationId_ReturnsFailedEntries()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);
            var operationId = "amer_9e0e3ba8-c562-440f-ba9d-10603ee31837";

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var response = await TeamsApiClient.GetPagedFailedEntriesAsync(
                        turnContext,
                        operationId,
                        cancellationToken: cancellationToken);

                    Assert.NotNull(response);
                },
                CancellationToken.None);
        }

        [Fact(Skip = "internal error")]
        public async Task CancelOperationAsync_WithOperationId_CancelsOperation()
        {
            var adapter = InitializeTeamsBotFrameworkHttpAdapter();
            var conversationReference = CreateConversationReference(_conversationId);
            var operationId = "amer_9e0e3ba8-c562-440f-ba9d-10603ee31837";

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    await TeamsApiClient.CancelOperationAsync(
                        turnContext,
                        operationId,
                        cancellationToken);

                    // If no exception is thrown, the operation succeeded
                    Assert.True(true);
                },
                CancellationToken.None);
        }

        private TeamsBotFrameworkHttpAdapter InitializeTeamsBotFrameworkHttpAdapter()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddEnvironmentVariables();

            IConfiguration configuration = builder.Build();

            ServiceCollection services = new();
            services.AddSingleton(configuration);
            services.AddTeamsBotFrameworkHttpAdapter();
            services.AddLogging((builder) => {
                builder.AddXUnit(_outputHelper);
                builder.AddFilter("System.Net", LogLevel.Warning);
                builder.AddFilter("Microsoft.Identity", LogLevel.Error);
                builder.AddFilter("Microsoft.Teams", LogLevel.Information);
            });

            var serviceProvider = services.BuildServiceProvider();
            TeamsBotFrameworkHttpAdapter adapter = (TeamsBotFrameworkHttpAdapter)serviceProvider.GetRequiredService<IBotFrameworkHttpAdapter>();
            return adapter;
        }

        private ConversationReference CreateConversationReference(string conversationId)
        {
            return new ConversationReference
            {
                ChannelId = "msteams",
                ServiceUrl = _serviceUrl,
                Conversation = new ConversationAccount
                {
                    Id = conversationId
                },
                User = new ChannelAccount()
                {
                    Properties =
                    {
                        { "agenticAppBlueprintId", _agenticAppBlueprintId },
                        { "agenticAppId", _agenticAppId },
                        { "agenticUserId", _agenticUserId },
                    }
                }
            };
        }
    }
}
