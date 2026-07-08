// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Apps.BotBuilder.UnitTests;

public class TeamsApiClientTests
{
    [Fact]
    public async Task GetPagedMembersAsync_PassesInboundRequestContext()
    {
        Mock<ConversationClient> mockConversationClient = new(
            new HttpClient(),
            NullLogger<ConversationClient>.Instance);
        mockConversationClient
            .Setup(c => c.GetConversationPagedMembersAsync(
                "conversation-id",
                It.Is<Uri>(u => u.ToString().TrimEnd('/') == "https://smba.trafficmanager.net/teams"),
                null,
                null,
                It.Is<BotRequestContext?>(c =>
                    c != null
                    && c.BotAppId == "recipient-bot-id"
                    && c.AgenticIdentity != null
                    && c.AgenticIdentity.AgenticAppId == "agentic-app-id"
                    && c.AgenticIdentity.AgenticUserId == "agentic-user-id"),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Microsoft.Teams.Core.PagedMembersResult
            {
                Members = new List<Microsoft.Teams.Core.Schema.ChannelAccount>
                {
                    new Microsoft.Teams.Core.Schema.ChannelAccount
                    {
                        Id = "member-id",
                        Name = "Member"
                    }
                }
            });

        Activity activity = new()
        {
            Type = ActivityTypes.Message,
            ChannelId = "msteams",
            ServiceUrl = "https://smba.trafficmanager.net/teams/",
            Conversation = new Microsoft.Bot.Schema.ConversationAccount { Id = "conversation-id" },
            Recipient = new Microsoft.Bot.Schema.ChannelAccount
            {
                Id = "28:recipient-account-id",
                Properties =
                {
                    ["botId"] = "28:recipient-bot-id",
                    ["agenticAppId"] = "agentic-app-id",
                    ["agenticUserId"] = "agentic-user-id"
                }
            }
        };
        Mock<ITurnContext> turnContext = new();
        turnContext.SetupGet(t => t.Activity).Returns(activity);
        turnContext.SetupGet(t => t.TurnState).Returns(new TurnContextStateCollection
        {
            [typeof(Microsoft.Bot.Connector.IConnectorClient).FullName!] = new CompatConnectorClient(new CompatConversations(mockConversationClient.Object))
        });

        Microsoft.Bot.Schema.Teams.TeamsPagedMembersResult pagedMembers =
            await TeamsApiClient.GetPagedMembersAsync(turnContext.Object, cancellationToken: CancellationToken.None);

        Assert.Single(pagedMembers.Members);
        mockConversationClient.VerifyAll();
    }
}
