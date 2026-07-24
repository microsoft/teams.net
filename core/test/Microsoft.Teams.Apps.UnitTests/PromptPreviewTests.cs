// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Clients;
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

        await ctx.SendAsync(new MessageActivityInput().WithText("response text").WithRecipient(inbound.From!, isTargeted: true));

        Assert.NotNull(captured.Value);
        TeamsActivityInput teamsActivity = (TeamsActivityInput)captured.Value!;
        TargetedMessageInfoEntity? entity = teamsActivity.Entities?.OfType<TargetedMessageInfoEntity>().SingleOrDefault();
        Assert.NotNull(entity);
        Assert.Equal("1772129782775", entity.MessageId);
    }

    [Fact]
    public async Task SendActivityAsync_StringOverload_Succeeds_InPersonalChat()
    {
        // The string overload constructs a non-targeted message, so the 1:1 guard cannot fire.
        // This test pins that behavior: plain string sends from a personal chat go through.
        TestHarness harness = CreateHarness();
        CaptureSlot captured = SetupCapture(harness);

        MessageActivity inbound = BuildInbound(targetedInbound: false, inboundId: "1234", convType: ConversationTypes.Personal);
        Context<MessageActivity> ctx = new(harness.App, inbound);

        await ctx.SendAsync("hello");

        Assert.NotNull(captured.Value);
    }

    [Fact]
    public async Task SendActivityAsync_DoesNotAddTargetedMessageInfo_WhenNotTargeted()
    {
        TestHarness harness = CreateHarness();
        CaptureSlot captured = SetupCapture(harness);

        MessageActivity inbound = BuildInbound(targetedInbound: false, inboundId: "1234", convType: ConversationTypes.GroupChat);
        Context<MessageActivity> ctx = new(harness.App, inbound);

        await ctx.SendAsync(new MessageActivityInput().WithText("hello"));

        Assert.NotNull(captured.Value);
        TeamsActivityInput teamsActivity = (TeamsActivityInput)captured.Value!;
        Assert.Null(teamsActivity.Entities?.OfType<TargetedMessageInfoEntity>().SingleOrDefault());
    }

    [Fact]
    public async Task SendActivityAsync_DoesNotDuplicate_WhenEntityAlreadyPresent()
    {
        TestHarness harness = CreateHarness();
        CaptureSlot captured = SetupCapture(harness);

        MessageActivity inbound = BuildInbound(targetedInbound: true, inboundId: "1772129782775", convType: ConversationTypes.GroupChat);
        Context<MessageActivity> ctx = new(harness.App, inbound);

        MessageActivityInput outbound = new MessageActivityInput()
            .WithText("response")
            .WithRecipient(inbound.From!, isTargeted: true)
            .AddEntity(new TargetedMessageInfoEntity { MessageId = "9999" })
            ;

        await ctx.SendAsync(outbound);

        Assert.NotNull(captured.Value);
        TeamsActivityInput teamsActivity = (TeamsActivityInput)captured.Value!;
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

        MessageActivityInput outbound = new MessageActivityInput().WithText("secret").WithRecipient(inbound.From!, isTargeted: true);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ctx.SendAsync(outbound));
        Assert.Contains("personal", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendActivityAsync_Succeeds_WhenNonTargetedMessage_InPersonalChat()
    {
        TestHarness harness = CreateHarness();
        CaptureSlot captured = SetupCapture(harness);

        MessageActivity inbound = BuildInbound(targetedInbound: false, inboundId: "1234", convType: ConversationTypes.Personal);
        Context<MessageActivity> ctx = new(harness.App, inbound);

        await ctx.SendAsync(new MessageActivityInput().WithText("hi"));

        Assert.NotNull(captured.Value);
    }

    [Fact]
    public async Task SendActivityAsync_Succeeds_WhenTargetedMessage_InGroupChat()
    {
        TestHarness harness = CreateHarness();
        CaptureSlot captured = SetupCapture(harness);

        MessageActivity inbound = BuildInbound(targetedInbound: false, inboundId: "1234", convType: ConversationTypes.GroupChat);
        Context<MessageActivity> ctx = new(harness.App, inbound);

        MessageActivityInput outbound = new MessageActivityInput().WithText("only you can see this").WithRecipient(inbound.From!, isTargeted: true);

        await ctx.SendAsync(outbound);

        Assert.NotNull(captured.Value);
    }

    // ==================== Helpers ====================

    private sealed class CaptureSlot
    {
        public CoreActivityInput? Value { get; set; }
    }

    private static MessageActivity BuildInbound(bool targetedInbound, string inboundId, ConversationType convType)
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
                It.IsAny<string>(),
                It.IsAny<CoreActivityInput>(),
                It.IsAny<Uri>(),
                It.IsAny<bool>(),
                It.IsAny<BotRequestContext?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, CoreActivityInput, Uri, bool, BotRequestContext?, Dictionary<string, string>?, CancellationToken>(
                (_, activity, _, _, _, _, _) => slot.Value = activity)
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
