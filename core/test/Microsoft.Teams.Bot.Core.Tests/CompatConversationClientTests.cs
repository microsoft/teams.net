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

        public CompatConversationClientTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");
            _conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");
        }

        [Fact(Skip = "not implemented")]
        public async Task GetMemberAsync()
        {

            var compatAdapter = InitializeCompatAdapter();
            ConversationReference conversationReference = new ConversationReference

            {
                ChannelId = "msteams",
                ServiceUrl = _serviceUrl,
                Conversation = new ConversationAccount
                {
                    Id = _conversationId
                }
            };

            await compatAdapter.ContinueConversationAsync(
                string.Empty, conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    TeamsChannelAccount member = await TeamsInfo.GetMemberAsync(turnContext, _userId, cancellationToken: cancellationToken);
                    Assert.NotNull(member);
                    Assert.Equal(_userId, member.Id);

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
                Bot = new ChannelAccount()
                {
                    Id = "28:fake-bot-id",
                    Properties =
                    {
                        ["aadObjectId"] = "fake-aad-object-id"
                    }
                }
            };

            await compatAdapter.ContinueConversationAsync(
                string.Empty, conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var result = await TeamsInfo.GetPagedMembersAsync(turnContext, cancellationToken: cancellationToken);
                    Assert.NotNull(result);
                    Assert.True(result.Members.Count > 0);
                    var m0 = result.Members[0];
                    Assert.Equal(_userId, m0.Id);

                }, CancellationToken.None);
        }

        [Fact(Skip = "not implemented")]
        public async Task GetMeetingInfo()
        {
            string meetingId = Environment.GetEnvironmentVariable("TEST_MEETINGID") ?? throw new InvalidOperationException("TEST_MEETINGID environment variable not set");
            var compatAdapter = InitializeCompatAdapter();
            ConversationReference conversationReference = new ConversationReference

            {
                ChannelId = "msteams",
                ServiceUrl = _serviceUrl,
                Conversation = new ConversationAccount
                {
                    Id = _conversationId
                }
            };

            await compatAdapter.ContinueConversationAsync(
                string.Empty, conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    var result = await TeamsInfo.GetMeetingInfoAsync(turnContext, meetingId, cancellationToken);
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
