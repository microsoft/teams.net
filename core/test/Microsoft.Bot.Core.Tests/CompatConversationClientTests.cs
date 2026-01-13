// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Core.Compat;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Bot.Core.Tests
{
    public class CompatConversationClientTests
    {
        string serviceUrl = "https://smba.trafficmanager.net/amer/";

        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");
        string conversationId = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set");

        [Fact]
        public async Task GetMemberAsync()
        {

            var compatAdapter = InitializeCompatAdapter();
            ConversationReference conversationReference = new ConversationReference

            {
                ChannelId = "msteams",
                ServiceUrl = serviceUrl,
                Conversation = new ConversationAccount
                {
                    Id = conversationId
                }
            };

            await compatAdapter.ContinueConversationAsync(
                string.Empty, conversationReference,
                async (turnContext, cancellationToken) =>
                {
                    TeamsChannelAccount member = await TeamsInfo.GetMemberAsync(turnContext, userId, cancellationToken: cancellationToken);
                    Assert.NotNull(member);
                    Assert.Equal(userId, member.Id);

                }, CancellationToken.None);
        }

        [Fact]
        public async Task GetPagedMembersAsync()
        {

            var compatAdapter = InitializeCompatAdapter();
            ConversationReference conversationReference = new ConversationReference

            {
                ChannelId = "msteams",
                ServiceUrl = serviceUrl,
                Conversation = new ConversationAccount
                {
                    Id = conversationId
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
                    Assert.Equal(userId, m0.Id);

                }, CancellationToken.None);
        }

        [Fact]
        public async Task GetMeetingInfo()
        {
            string meetingId = Environment.GetEnvironmentVariable("TEST_MEETINGID") ?? throw new InvalidOperationException("TEST_MEETINGID environment variable not set");
            var compatAdapter = InitializeCompatAdapter();
            ConversationReference conversationReference = new ConversationReference

            {
                ChannelId = "msteams",
                ServiceUrl = serviceUrl,
                Conversation = new ConversationAccount
                {
                    Id = conversationId
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
            services.AddSingleton<ILogger<BotApplication>>(NullLogger<BotApplication>.Instance);
            services.AddSingleton<ILogger<ConversationClient>>(NullLogger<ConversationClient>.Instance);
            services.AddSingleton(configuration);
            services.AddCompatAdapter();
            services.AddLogging(configure => configure.AddConsole());

            var serviceProvider = services.BuildServiceProvider();
            CompatAdapter compatAdapter = (CompatAdapter)serviceProvider.GetRequiredService<IBotFrameworkHttpAdapter>();
            return compatAdapter;
        }
    }
}
