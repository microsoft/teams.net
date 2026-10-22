// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
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

public class ContextReplyChainPlacementTests
{
    private const string ChatId = "19:user_bot@thread.v2";
    private const string ChannelId = "19:channel@thread.tacv2";
    private const string GroupChatId = "19:group@thread.v2";
    private const string OpaqueConversationId = "a:opaque-inbound-conversation";

    [Fact]
    public async Task Send_InboundInsideReplyChain_RepliesInThatChainUsingPayloadIds()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: OpaqueConversationId,
            id: "reply-2",
            channelData: new { chat = new { id = ChatId }, replyChain = new { id = "root-1" } }));

        await context.SendAsync("hi");

        Assert.Equal(ChatId, harness.Reply.ConversationId);
        Assert.Equal("root-1", harness.Reply.ActivityId);
        Assert.False(harness.Reply.IsTargeted);
        Assert.Equal(1, harness.Reply.Calls);
        Assert.Equal(0, harness.Send.Calls);
    }

    [Fact]
    public async Task Send_InboundTopLevelMessage_SendsToInboundConversation()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: OpaqueConversationId,
            id: "root-1",
            channelData: new { chat = new { id = ChatId }, replyChain = new { id = "root-1" } }));

        await context.SendAsync("hi");

        Assert.Equal(OpaqueConversationId, harness.Send.ConversationId);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Theory]
    [InlineData("{ \"chat\": { \"id\": \"" + ChatId + "\" } }")]
    [InlineData("{ \"replyChain\": { \"id\": \"root-1\" } }")]
    [InlineData("{ \"chat\": { \"id\": \"" + ChatId + "\" }, \"replyChain\": {} }")]
    [InlineData("{ \"chat\": {}, \"replyChain\": { \"id\": \"root-1\" } }")]
    [InlineData("{ \"chat\": { \"id\": \" \" }, \"replyChain\": { \"id\": \"root-1\" } }")]
    [InlineData("{ \"chat\": { \"id\": \"" + ChatId + "\" }, \"replyChain\": { \"id\": \" \" } }")]
    [InlineData("{}")]
    public async Task Send_PayloadMissingAnId_SendsToInboundConversation(string channelDataJson)
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: OpaqueConversationId,
            id: "reply-2",
            channelData: JsonDocument.Parse(channelDataJson).RootElement));

        await context.SendAsync("hi");

        Assert.Equal(OpaqueConversationId, harness.Send.ConversationId);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Fact]
    public async Task Send_ChannelReplyWithoutReplyChain_SendsToInboundConversationUnchanged()
    {
        Harness harness = CreateHarness();
        string inboundConversationId = $"{ChannelId};messageid=root-1";
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: inboundConversationId,
            id: "reply-2",
            channelData: new { channel = new { id = ChannelId } }));

        await context.SendAsync("hi");

        Assert.Equal(inboundConversationId, harness.Send.ConversationId);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Fact]
    public async Task Send_ChannelRootPost_SendsToInboundConversationUnchanged()
    {
        Harness harness = CreateHarness();
        string inboundConversationId = $"{ChannelId};messageid=root-1";
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: inboundConversationId,
            id: "root-1",
            channelData: new { channel = new { id = ChannelId }, replyChain = new { id = "root-1" } }));

        await context.SendAsync("hi");

        Assert.Equal(inboundConversationId, harness.Send.ConversationId);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Fact]
    public async Task Send_ChannelReplyWithReplyChainButNoChatId_SendsToInboundConversationUnchanged()
    {
        Harness harness = CreateHarness();
        string inboundConversationId = $"{ChannelId};messageid=root-1";
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: inboundConversationId,
            id: "reply-2",
            channelData: new { channel = new { id = ChannelId }, replyChain = new { id = "root-1" } }));

        await context.SendAsync("hi");

        Assert.Equal(inboundConversationId, harness.Send.ConversationId);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Fact]
    public async Task Typing_InboundInsideReplyChain_IsNotPlaced()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: OpaqueConversationId,
            id: "reply-2",
            channelData: new { chat = new { id = ChatId }, replyChain = new { id = "root-1" } }));

        await context.TypingAsync();

        Assert.Equal(OpaqueConversationId, harness.Send.ConversationId);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Fact]
    public async Task Send_NonMessageInbound_SendsToInboundConversation()
    {
        Harness harness = CreateHarness();
        InvokeActivity invoke = new(InvokeNames.TaskFetch)
        {
            Id = "invoke-2",
            ServiceUrl = Harness.ServiceUrl,
            Conversation = new TeamsConversation { Id = OpaqueConversationId },
            ChannelData = JsonSerializer.Deserialize<TeamsChannelData>("{\"chat\":{\"id\":\"" + ChatId + "\"},\"replyChain\":{\"id\":\"root-1\"}}"),
        };
        Context<InvokeActivity> context = new(harness.App, invoke);

        await context.SendAsync("hi");

        Assert.Equal(OpaqueConversationId, harness.Send.ConversationId);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Fact]
    public async Task Send_ExplicitlyTargetedMessageInsideReplyChain_RepliesInThatChainAsTargeted()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: OpaqueConversationId,
            id: "reply-2",
            channelData: new { chat = new { id = ChatId }, replyChain = new { id = "root-1" } }));

        await context.SendAsync(new MessageActivityInput()
            .WithText("only you")
            .WithRecipient(new TeamsChannelAccount { Id = "user-1" }, isTargeted: true));

        Assert.Equal(ChatId, harness.Reply.ConversationId);
        Assert.Equal("root-1", harness.Reply.ActivityId);
        Assert.True(harness.Reply.IsTargeted);
        Assert.Equal(0, harness.Send.Calls);
    }

    [Fact]
    public async Task Reply_InboundInsideReplyChain_KeepsTheQuoteAndReplyInThatChain()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: OpaqueConversationId,
            id: "reply-2",
            channelData: new { chat = new { id = ChatId }, replyChain = new { id = "root-1" } }));

        await context.ReplyAsync("hi");

        Assert.Equal(ChatId, harness.Reply.ConversationId);
        Assert.Equal("root-1", harness.Reply.ActivityId);
        Assert.Contains("quotedReply", harness.Reply.Activity!.ToJson(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_ReplyChainCallDoesNotSetReplyToId()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: OpaqueConversationId,
            id: "reply-2",
            channelData: new { chat = new { id = ChatId }, replyChain = new { id = "root-1" } }));

        await context.SendAsync("hi");

        Assert.Null(harness.Reply.Activity?.ReplyToId);
    }

    [Theory]
    [InlineData("personal")]
    [InlineData("channel")]
    public async Task Send_InboundInsideReplyChain_RepliesInThatChainForEveryConversationTypeButGroupChat(string conversationType)
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: OpaqueConversationId,
            id: "reply-2",
            channelData: new { chat = new { id = ChatId }, replyChain = new { id = "root-1" } },
            conversationType: conversationType));

        await context.SendAsync("hi");

        Assert.Equal(ChatId, harness.Reply.ConversationId);
        Assert.Equal("root-1", harness.Reply.ActivityId);
        Assert.Equal(0, harness.Send.Calls);
    }

    [Fact]
    public async Task Send_GroupChatInboundInsideReplyChain_SendsToInboundConversationEvenWhenThePayloadCarriesBothIds()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: GroupChatId,
            id: "reply-2",
            channelData: new { chat = new { id = GroupChatId }, replyChain = new { id = "root-1" } },
            conversationType: "groupChat"));

        await context.SendAsync("hi");

        Assert.Equal(GroupChatId, harness.Send.ConversationId);
        Assert.Equal(1, harness.Send.Calls);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Fact]
    public async Task Send_MeetingChatInboundInsideReplyChain_SendsToInboundConversation()
    {
        const string MeetingChatId = "19:meeting_abc@thread.v2";
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: MeetingChatId,
            id: "reply-2",
            channelData: new { chat = new { id = MeetingChatId }, replyChain = new { id = "root-1" }, meeting = new { id = "meeting-1" } },
            conversationType: "groupChat"));

        await context.SendAsync("hi");

        Assert.Equal(MeetingChatId, harness.Send.ConversationId);
        Assert.Equal(1, harness.Send.Calls);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Fact]
    public async Task Send_GroupChatInboundInsideReplyChain_TargetedMessageGoesToTheInboundConversation()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: GroupChatId,
            id: "reply-2",
            channelData: new { chat = new { id = GroupChatId }, replyChain = new { id = "root-1" } },
            conversationType: "groupChat"));

        await context.SendAsync(new MessageActivityInput()
            .WithText("only you")
            .WithRecipient(new TeamsChannelAccount { Id = "user-1" }, isTargeted: true));

        Assert.Equal(GroupChatId, harness.Send.ConversationId);
        Assert.True(harness.Send.IsTargeted);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Fact]
    public async Task Reply_GroupChatInboundInsideReplyChain_QuotesAndSendsToTheInboundConversation()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: GroupChatId,
            id: "reply-2",
            channelData: new { chat = new { id = GroupChatId }, replyChain = new { id = "root-1" } },
            conversationType: "groupChat"));

        await context.ReplyAsync("hi");

        Assert.Equal(GroupChatId, harness.Send.ConversationId);
        Assert.Contains("quotedReply", harness.Send.Activity!.ToJson(), StringComparison.Ordinal);
        Assert.Equal(0, harness.Reply.Calls);
    }

    private static MessageActivity Inbound(string conversationId, string id, object channelData, string? conversationType = null)
    {
        object conversation = conversationType is null
            ? new { id = conversationId }
            : new { id = conversationId, conversationType };
        string json = JsonSerializer.Serialize(new
        {
            type = "message",
            id,
            channelId = "msteams",
            serviceUrl = Harness.ServiceUrl.ToString(),
            from = new { id = "user-1", name = "User" },
            recipient = new { id = "bot-1", name = "Bot" },
            conversation,
            text = "inbound text",
            channelData,
        });

        return (MessageActivity)TeamsActivity.FromActivity(CoreActivity.FromJsonString(json));
    }

    private sealed class Capture
    {
        public int Calls { get; set; }
        public string? ConversationId { get; set; }
        public string? ActivityId { get; set; }
        public CoreActivityInput? Activity { get; set; }
        public bool IsTargeted { get; set; }
    }

    private sealed class Harness
    {
        public static readonly Uri ServiceUrl = new("https://smba.trafficmanager.net/test/");

        public required TeamsBotApplication App { get; init; }
        public required Capture Send { get; init; }
        public required Capture Reply { get; init; }

        public Context<MessageActivity> ContextFor(MessageActivity inbound) => new(App, inbound);
    }

    private static Harness CreateHarness()
    {
        Mock<UserTokenClient> userTokenClient = new(
            new HttpClient(),
            new Mock<IConfiguration>().Object,
            NullLogger<UserTokenClient>.Instance);
        Mock<ConversationClient> conversationClient = new(new HttpClient(), NullLogger<ConversationClient>.Instance);
        Capture send = new();
        Capture reply = new();

        conversationClient
            .Setup(c => c.SendActivityAsync(
                It.IsAny<string>(),
                It.IsAny<CoreActivityInput>(),
                It.IsAny<Uri>(),
                It.IsAny<bool>(),
                It.IsAny<BotRequestContext?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, CoreActivityInput, Uri, bool, BotRequestContext?, Dictionary<string, string>?, CancellationToken>(
                (conversationId, activity, _, isTargeted, _, _, _) =>
                {
                    send.Calls++;
                    send.ConversationId = conversationId;
                    send.Activity = activity;
                    send.IsTargeted = isTargeted;
                })
            .ReturnsAsync(new SendActivityResponse { Id = "sent-id" });

        conversationClient
            .Setup(c => c.SendReplyChainActivityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CoreActivityInput>(),
                It.IsAny<Uri>(),
                It.IsAny<bool>(),
                It.IsAny<BotRequestContext?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, CoreActivityInput, Uri, bool, BotRequestContext?, Dictionary<string, string>?, CancellationToken>(
                (conversationId, activityId, activity, _, isTargeted, _, _, _) =>
                {
                    reply.Calls++;
                    reply.ConversationId = conversationId;
                    reply.ActivityId = activityId;
                    reply.Activity = activity;
                    reply.IsTargeted = isTargeted;
                })
            .ReturnsAsync(new SendActivityResponse { Id = "reply-id" });

        TeamsBotApplication app = new(
            new ApiClient(new HttpClient(), conversationClient.Object, userTokenClient.Object),
            new HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "test-app-id" });

        return new Harness { App = app, Send = send, Reply = reply };
    }
}
