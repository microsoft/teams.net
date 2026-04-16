// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Compat;
using Microsoft.Teams.Bot.Core;
using Xunit.Abstractions;

namespace Microsoft.Bot.Core.Tests
{
    public class CompatConversationClientTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly string _serviceUrl = "https://smba.trafficmanager.net/amer/";
        private readonly string _userId;
        private readonly string _conversationId;
        private readonly string _agenticAppBlueprintId;
        private readonly string? _agenticAppId;
        private readonly string? _agenticUserId;

        public CompatConversationClientTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");
            _conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");

            _agenticAppBlueprintId = Environment.GetEnvironmentVariable("AzureAd__ClientId") ?? throw new InvalidOperationException("AzureAd__ClientId environment variable not set");
            _agenticAppId = Environment.GetEnvironmentVariable("TEST_AGENTIC_APPID");// ?? throw new InvalidOperationException("TEST_AGENTIC_APPID environment variable not set");
            _agenticUserId = Environment.GetEnvironmentVariable("TEST_AGENTIC_USERID");// ?? throw new InvalidOperationException("TEST_AGENTIC_USERID environment variable not set");
        }

        [Fact]
        public async Task GetMemberAsync()
        {
            // TeamsInfo.GetMemberAsync hard-casts IConversations to the concrete Conversations class,
            // which is incompatible with CompatConversations. Use CompatTeamsInfo instead.
            var compatAdapter = InitializeCompatAdapter();
            ConversationReference conversationReference = new ConversationReference
            {
                ChannelId = "msteams",
                ServiceUrl = _serviceUrl,
                Conversation = new ConversationAccount
                {
                    Id = _conversationId
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

            await compatAdapter.ContinueConversationAsync(
                string.Empty, conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    // Resolve pairwise MRI first
                    var pagedResult = await CompatTeamsInfo.GetPagedMembersAsync(turnContext, cancellationToken: cancellationToken);
                    string aadUserId = _userId.Replace("29:", "");
                    var matchedMember = pagedResult.Members.FirstOrDefault(m =>
                        string.Equals(m.AadObjectId, aadUserId, StringComparison.OrdinalIgnoreCase));
                    Assert.NotNull(matchedMember);

                    TeamsChannelAccount member = await CompatTeamsInfo.GetMemberAsync(turnContext, matchedMember.Id, cancellationToken: cancellationToken);
                    Assert.NotNull(member);
                    Assert.Equal(matchedMember.Id, member.Id);

                }, CancellationToken.None);
        }

        [Fact]
        public async Task GetPagedMembersAsync()
        {

            var compatAdapter = InitializeCompatAdapter();
            ConversationReference conversationReference = new ConversationReference

            {
                ChannelId = "msteams",
                ServiceUrl = _serviceUrl,
                Conversation = new ConversationAccount()
                {
                    Id = _conversationId
                },
                User = new ChannelAccount()
                {
                    Id = "28:fake-bot-id",
                    Properties =
                    {
                        ["agenticAppId"] = _agenticAppId,
                        ["agenticUserId"] = _agenticUserId,
                        ["agenticAppBlueprintId"] = _agenticAppBlueprintId
                    }
                }
            };

            await compatAdapter.ContinueConversationAsync(
                string.Empty, conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var result = await CompatTeamsInfo.GetPagedMembersAsync(turnContext, cancellationToken: cancellationToken);
                    Assert.NotNull(result);
                    Assert.True(result.Members.Count > 0);
                    // Member IDs are pairwise-encrypted MRIs, not the AAD-based TEST_USER_ID
                    var m0 = result.Members[0];
                    Assert.NotNull(m0.Id);
                    Assert.StartsWith("29:", m0.Id);

                }, CancellationToken.None);
        }

        [Trait("Category", "needs-meeting-context")]
        [Fact]
        public async Task GetMeetingInfo()
        {
            // TeamsInfo.GetMeetingInfoAsync uses TeamsConnectorClient which requires real credentials.
            // Use CompatTeamsInfo instead, which delegates to the Core SDK's ConversationClient.
            string meetingId = Environment.GetEnvironmentVariable("TEST_MEETINGID") ?? throw new InvalidOperationException("TEST_MEETINGID environment variable not set");
            var compatAdapter = InitializeCompatAdapter();
            ConversationReference conversationReference = new ConversationReference
            {
                ChannelId = "msteams",
                ServiceUrl = _serviceUrl,
                Conversation = new ConversationAccount
                {
                    Id = _conversationId
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

            await compatAdapter.ContinueConversationAsync(
                string.Empty, conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var result = await CompatTeamsInfo.GetMeetingInfoAsync(turnContext, meetingId, cancellationToken);
                    Assert.NotNull(result);

                }, CancellationToken.None);
        }


        CompatAdapter InitializeCompatAdapter()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                            .AddEnvironmentVariables();

            IConfiguration configuration = builder.Build();

            ServiceCollection services = new();
            services.AddSingleton(configuration);
            services.AddCompatAdapter();
            services.AddLogging((builder) => {
                builder.AddXUnit(_outputHelper);
                builder.AddFilter("System.Net", LogLevel.Warning);
                builder.AddFilter("Microsoft.Identity", LogLevel.Error);
                builder.AddFilter("Microsoft.Teams", LogLevel.Information);
            });

            var serviceProvider = services.BuildServiceProvider();
            CompatAdapter compatAdapter = (CompatAdapter)serviceProvider.GetRequiredService<IBotFrameworkHttpAdapter>();
            return compatAdapter;
        }
    }
}
