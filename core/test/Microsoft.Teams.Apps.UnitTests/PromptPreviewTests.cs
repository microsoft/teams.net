// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

public class PromptPreviewTests
{
    [Fact]
    public async Task SendActivityAsync_AutoPopulatesTargetedMessageInfo_WhenInboundIsTargeted()
    {
        TestHarness harness = CreateHarness();
        CaptureSlot captured = SetupCapture(harness);

        MessageActivity inbound = BuildInbound(targetedInbound: true, inboundId: "1772129782775", convType: ConversationTypes.GroupChat);
        Context<MessageActivity> ctx = new(harness.App, inbound);

        await ctx.SendActivityAsync(new MessageActivity("response text"));

        Assert.NotNull(captured.Value);
        TeamsActivity teamsActivity = (TeamsActivity)captured.Value!;
        TargetedMessageInfoEntity? entity = teamsActivity.Entities?.OfType<TargetedMessageInfoEntity>().SingleOrDefault();
        Assert.NotNull(entity);
        Assert.Equal("1772129782775", entity.MessageId);
    }

    [Fact]
    public async Task SendActivityAsync_StringOverload_AutoPopulatesTargetedMessageInfo_WhenInboundIsTargeted()
    {
        TestHarness harness = CreateHarness();
        CaptureSlot captured = SetupCapture(harness);

        MessageActivity inbound = BuildInbound(targetedInbound: true, inboundId: "1772129782775", convType: ConversationTypes.GroupChat);
        Context<MessageActivity> ctx = new(harness.App, inbound);

        await ctx.SendActivityAsync("plain text response");

        Assert.NotNull(captured.Value);
        TeamsActivity teamsActivity = (TeamsActivity)captured.Value!;
        TargetedMessageInfoEntity? entity = teamsActivity.Entities?.OfType<TargetedMessageInfoEntity>().SingleOrDefault();
        Assert.NotNull(entity);
        Assert.Equal("1772129782775", entity.MessageId);
    }

    [Fact]
    public async Task SendActivityAsync_StringOverload_Succeeds_InPersonalChat()
    {
        // The string overload constructs a MessageActivity with no recipient, so IsTargeted is
        // never set and the 1:1 guard cannot fire. This test pins that behavior: plain string
        // sends from a personal chat go through without throwing, even though the typed overload
        // would throw if a caller explicitly built a targeted MessageActivity.
        TestHarness harness = CreateHarness();
        CaptureSlot captured = SetupCapture(harness);

        MessageActivity inbound = BuildInbound(targetedInbound: false, inboundId: "1234", convType: ConversationTypes.Personal);
        Context<MessageActivity> ctx = new(harness.App, inbound);

        await ctx.SendActivityAsync("hello");

        Assert.NotNull(captured.Value);
    }

    [Fact]
    public async Task SendActivityAsync_DoesNotAddTargetedMessageInfo_WhenInboundNotTargeted()
    {
        TestHarness harness = CreateHarness();
        CaptureSlot captured = SetupCapture(harness);

        MessageActivity inbound = BuildInbound(targetedInbound: false, inboundId: "1234", convType: ConversationTypes.GroupChat);
        Context<MessageActivity> ctx = new(harness.App, inbound);

        await ctx.SendActivityAsync(new MessageActivity("hello"));

        Assert.NotNull(captured.Value);
        TeamsActivity teamsActivity = (TeamsActivity)captured.Value!;
        Assert.Null(teamsActivity.Entities?.OfType<TargetedMessageInfoEntity>().SingleOrDefault());
    }

    [Fact]
    public async Task SendActivityAsync_DoesNotDuplicate_WhenEntityAlreadyPresent()
    {
        TestHarness harness = CreateHarness();
        CaptureSlot captured = SetupCapture(harness);

        MessageActivity inbound = BuildInbound(targetedInbound: true, inboundId: "1772129782775", convType: ConversationTypes.GroupChat);
        Context<MessageActivity> ctx = new(harness.App, inbound);

        MessageActivity outbound = new("response");
        outbound.AddEntity(new TargetedMessageInfoEntity { MessageId = "9999" });

        await ctx.SendActivityAsync(outbound);

        Assert.NotNull(captured.Value);
        TeamsActivity teamsActivity = (TeamsActivity)captured.Value!;
        List<TargetedMessageInfoEntity> entities = teamsActivity.Entities!.OfType<TargetedMessageInfoEntity>().ToList();
        Assert.Single(entities);
        Assert.Equal("9999", entities[0].MessageId);
    }

    [Fact]
    public async Task SendActivityAsync_Throws_WhenTargetedMessage_InPersonalChat()
    {
        TestHarness harness = CreateHarness();
        SetupCapture(harness);

        MessageActivity inbound = BuildInbound(targetedInbound: false, inboundId: "1234", convType: ConversationTypes.Personal);
        Context<MessageActivity> ctx = new(harness.App, inbound);

        MessageActivity outbound = new("secret");
        outbound.Recipient = new TeamsChannelAccount { Id = "user-1", IsTargeted = true };

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ctx.SendActivityAsync(outbound));
        Assert.Contains("personal", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendActivityAsync_Succeeds_WhenNonTargetedMessage_InPersonalChat()
    {
        TestHarness harness = CreateHarness();
        CaptureSlot captured = SetupCapture(harness);

        MessageActivity inbound = BuildInbound(targetedInbound: false, inboundId: "1234", convType: ConversationTypes.Personal);
        Context<MessageActivity> ctx = new(harness.App, inbound);

        await ctx.SendActivityAsync(new MessageActivity("hi"));

        Assert.NotNull(captured.Value);
    }

    [Fact]
    public async Task SendActivityAsync_Succeeds_WhenTargetedMessage_InGroupChat()
    {
        TestHarness harness = CreateHarness();
        CaptureSlot captured = SetupCapture(harness);

        MessageActivity inbound = BuildInbound(targetedInbound: false, inboundId: "1234", convType: ConversationTypes.GroupChat);
        Context<MessageActivity> ctx = new(harness.App, inbound);

        MessageActivity outbound = new("only you can see this");
        outbound.Recipient = new TeamsChannelAccount { Id = "user-1", IsTargeted = true };

        await ctx.SendActivityAsync(outbound);

        Assert.NotNull(captured.Value);
    }

    // ==================== Helpers ====================

    private sealed class CaptureSlot
    {
        public CoreActivity? Value { get; set; }
    }

    private static MessageActivity BuildInbound(bool targetedInbound, string inboundId, string convType)
    {
        return new MessageActivity("inbound text")
        {
            Id = inboundId,
            ChannelId = "msteams",
            ServiceUrl = new Uri("https://smba.trafficmanager.net/test/"),
            From = new TeamsChannelAccount { Id = "user-1", Name = "User" },
            Recipient = new TeamsChannelAccount
            {
                Id = "bot-1",
                Name = "Bot",
                IsTargeted = targetedInbound ? true : null
            },
            Conversation = new TeamsConversation
            {
                Id = "conv-1",
                ConversationType = convType
            }
        };
    }

    private static CaptureSlot SetupCapture(TestHarness harness)
    {
        CaptureSlot slot = new();
        harness.MockConversationClient
            .Setup(c => c.SendActivityAsync(
                It.IsAny<CoreActivity>(),
                It.IsAny<BotRequestContext?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<CoreActivity, BotRequestContext?, Dictionary<string, string>?, CancellationToken>(
                (activity, _, _, _) => slot.Value = activity)
            .ReturnsAsync(new SendActivityResponse { Id = "sent-id" });
        return slot;
    }

    private sealed class TestHarness
    {
        public required TeamsBotApplication App { get; init; }
        public required Mock<ConversationClient> MockConversationClient { get; init; }
    }

    private static TestHarness CreateHarness()
    {
        Mock<IConfiguration> mockConfig = new();
        Mock<UserTokenClient> mockUserTokenClient = new(
            new HttpClient(),
            mockConfig.Object,
            NullLogger<UserTokenClient>.Instance);
        Mock<ConversationClient> mockConversationClient = new(new HttpClient(), NullLogger<ConversationClient>.Instance);

        ApiClient apiClient = new(
            new HttpClient(),
            mockConversationClient.Object,
            mockUserTokenClient.Object);

        TeamsBotApplication app = new(
            apiClient,
            new HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "test-app-id" });

        return new TestHarness
        {
            App = app,
            MockConversationClient = mockConversationClient,
        };
    }
}
