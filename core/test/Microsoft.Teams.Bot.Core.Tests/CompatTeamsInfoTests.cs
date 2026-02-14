// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Bot.Compat;
using Microsoft.Teams.Bot.Core;

namespace Microsoft.Bot.Core.Tests
{
    /// <summary>
    /// Integration tests for CompatTeamsInfo static methods.
    /// These tests verify that the compatibility layer correctly adapts
    /// Bot Framework TeamsInfo API to Teams Bot Core SDK.
    /// </summary>
    public class CompatTeamsInfoTests
    {
        private readonly string _serviceUrl = "https://smba.trafficmanager.net/amer/";
        private readonly string _userId;
        private readonly string _conversationId;
        private readonly string _teamId;
        private readonly string _channelId;
        private readonly string _meetingId;
        private readonly string _tenantId;

        public CompatTeamsInfoTests()
        {
            // These tests require environment variables for live integration testing
            _userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? "29:test-user-id";
            _conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? "19:test-conversation-id";
            _teamId = Environment.GetEnvironmentVariable("TEST_TEAMID") ?? "19:test-team-id";
            _channelId = Environment.GetEnvironmentVariable("TEST_CHANNELID") ?? "19:test-channel-id";
            _meetingId = Environment.GetEnvironmentVariable("TEST_MEETINGID") ?? "test-meeting-id";
            _tenantId = Environment.GetEnvironmentVariable("TEST_TENANTID") ?? "test-tenant-id";
        }

        [Fact]
        public async Task GetMemberAsync_WithValidUserId_ReturnsMember()
        {
            var adapter = InitializeCompatAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    TeamsChannelAccount member = await CompatTeamsInfo.GetMemberAsync(
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
            var adapter = InitializeCompatAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    var members = await CompatTeamsInfo.GetMembersAsync(turnContext, cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete

                    Assert.NotNull(members);
                    Assert.NotEmpty(members);
                },
                CancellationToken.None);
        }

        [Fact]
        public async Task GetPagedMembersAsync_ReturnsPagedResult()
        {
            var adapter = InitializeCompatAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var result = await CompatTeamsInfo.GetPagedMembersAsync(
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
            var adapter = InitializeCompatAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var member = await CompatTeamsInfo.GetTeamMemberAsync(
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
            var adapter = InitializeCompatAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    var members = await CompatTeamsInfo.GetTeamMembersAsync(
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
            var adapter = InitializeCompatAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var result = await CompatTeamsInfo.GetPagedTeamMembersAsync(
                        turnContext,
                        _teamId,
                        pageSize: 5,
                        cancellationToken: cancellationToken);

                    Assert.NotNull(result);
                    Assert.NotNull(result.Members);
                },
                CancellationToken.None);
        }

        [Fact(Skip = "Require special permissions")]
        public async Task GetMeetingInfoAsync_WithMeetingId_ReturnsMeetingInfo()
        {
            var adapter = InitializeCompatAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var meetingInfo = await CompatTeamsInfo.GetMeetingInfoAsync(
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
            var adapter = InitializeCompatAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var participant = await CompatTeamsInfo.GetMeetingParticipantAsync(
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

        [Fact(Skip = "Require special permissions")]
        public async Task SendMeetingNotificationAsync_SendsNotification()
        {
            var adapter = InitializeCompatAdapter();
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

                    var response = await CompatTeamsInfo.SendMeetingNotificationAsync(
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
            var adapter = InitializeCompatAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var teamDetails = await CompatTeamsInfo.GetTeamDetailsAsync(
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
            var adapter = InitializeCompatAdapter();
            var conversationReference = CreateConversationReference(_conversationId);

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var channels = await CompatTeamsInfo.GetTeamChannelsAsync(
                        turnContext,
                        _teamId,
                        cancellationToken);

                    Assert.NotNull(channels);
                    Assert.NotEmpty(channels);

                    var firstChannel = channels[0];
                    Assert.NotNull(firstChannel.Id);
                    Assert.NotNull(firstChannel.Name);
                },
                CancellationToken.None);
        }

        [Fact(Skip = "Require special permissions")]
        public async Task SendMessageToListOfUsersAsync_ReturnsOperationId()
        {
            var adapter = InitializeCompatAdapter();
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
                        new TeamMember(_userId),
                        new TeamMember(_userId),
                        new TeamMember(_userId),
                        new TeamMember(_userId),
                        new TeamMember(_userId)

                    };

                    var operationId = await CompatTeamsInfo.SendMessageToListOfUsersAsync(
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

        [Fact(Skip = "Require special permissions")]
        public async Task SendMessageToListOfChannelsAsync_ReturnsOperationId()
        {
            var adapter = InitializeCompatAdapter();
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
                        new TeamMember(_channelId)
                    };

                    var operationId = await CompatTeamsInfo.SendMessageToListOfChannelsAsync(
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

        [Fact(Skip = "Require special permissions")]
        public async Task SendMessageToAllUsersInTeamAsync_ReturnsOperationId()
        {
            var adapter = InitializeCompatAdapter();
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

                    var operationId = await CompatTeamsInfo.SendMessageToAllUsersInTeamAsync(
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

        [Fact]
        public async Task SendMessageToAllUsersInTenantAsync_ReturnsOperationId()
        {
            var adapter = InitializeCompatAdapter();
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

                    var operationId = await CompatTeamsInfo.SendMessageToAllUsersInTenantAsync(
                        turnContext,
                        activity,
                        _tenantId,
                        cancellationToken);

                    Assert.NotNull(operationId);
                    Assert.NotEmpty(operationId);
                },
                CancellationToken.None);
        }

        [Fact(Skip = "Require special permissions")]
        public async Task SendMessageToTeamsChannelAsync_CreatesConversationAndSendsMessage()
        {
            var adapter = InitializeCompatAdapter();
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
                    var botAppId = Environment.GetEnvironmentVariable("MicrosoftAppId") ?? string.Empty;

                    var result = await CompatTeamsInfo.SendMessageToTeamsChannelAsync(
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

        [Fact(Skip = "Require special permissions")]
        public async Task GetOperationStateAsync_WithOperationId_ReturnsState()
        {
            var adapter = InitializeCompatAdapter();
            var conversationReference = CreateConversationReference(_conversationId);
            var operationId = "amer_9e0e3ba8-c562-440f-ba9d-10603ee31837";

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var state = await CompatTeamsInfo.GetOperationStateAsync(
                        turnContext,
                        operationId,
                        cancellationToken);

                    Assert.NotNull(state);
                    Assert.NotNull(state.State);
                },
                CancellationToken.None);
        }

        [Fact(Skip = "Require special permissions")]
        public async Task GetPagedFailedEntriesAsync_WithOperationId_ReturnsFailedEntries()
        {
            var adapter = InitializeCompatAdapter();
            var conversationReference = CreateConversationReference(_conversationId);
            var operationId = "amer_9e0e3ba8-c562-440f-ba9d-10603ee31837";

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var response = await CompatTeamsInfo.GetPagedFailedEntriesAsync(
                        turnContext,
                        operationId,
                        cancellationToken: cancellationToken);

                    Assert.NotNull(response);
                },
                CancellationToken.None);
        }

        [Fact(Skip = "Requires valid operation ID from batch operation")]
        public async Task CancelOperationAsync_WithOperationId_CancelsOperation()
        {
            var adapter = InitializeCompatAdapter();
            var conversationReference = CreateConversationReference(_conversationId);
            var operationId = "amer_9e0e3ba8-c562-440f-ba9d-10603ee31837";

            await adapter.ContinueConversationAsync(
                string.Empty,
                conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    await CompatTeamsInfo.CancelOperationAsync(
                        turnContext,
                        operationId,
                        cancellationToken);

                    // If no exception is thrown, the operation succeeded
                    Assert.True(true);
                },
                CancellationToken.None);
        }

        private CompatAdapter InitializeCompatAdapter()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddEnvironmentVariables();

            IConfiguration configuration = builder.Build();

            ServiceCollection services = new();
            services.AddSingleton<ILogger<BotApplication>>(NullLogger<BotApplication>.Instance);
            services.AddSingleton<ILogger<ConversationClient>>(NullLogger<ConversationClient>.Instance);
            services.AddSingleton(configuration);
            services.AddCompatAdapter();
            services.AddLogging(configure => configure.AddConsole());

            var serviceProvider = services.BuildServiceProvider();
            CompatAdapter compatAdapter = (CompatAdapter)serviceProvider.GetRequiredService<IBotFrameworkHttpAdapter>();
            return compatAdapter;
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
                }
            };
        }
    }
}
