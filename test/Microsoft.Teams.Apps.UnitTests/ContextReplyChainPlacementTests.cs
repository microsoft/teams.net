// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    private const string ChannelThreadConversationId = ChannelId + ";messageid=root-1";
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
            channelData: new { channel = new { id = ChannelId } },
            conversationType: "channel"));

        await context.SendAsync("hi");

        Assert.Equal(inboundConversationId, harness.Send.ConversationId);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Fact]
    public async Task Send_ChannelWithWhitespaceChannelId_SendsToInboundConversationUnchanged()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: ChannelThreadConversationId,
            id: "reply-2",
            channelData: new { channel = new { id = " " }, replyChain = new { id = "root-1" } },
            conversationType: "channel"));

        await context.SendAsync("hi");

        Assert.Equal(ChannelThreadConversationId, harness.Send.ConversationId);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Fact]
    public async Task Send_ChannelRootPost_RepliesInTheReplyChainOfThePost()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: $"{ChannelId};messageid=root-1",
            id: "root-1",
            channelData: new { channel = new { id = ChannelId }, replyChain = new { id = "root-1" } },
            conversationType: "channel"));

        await context.SendAsync("hi");

        Assert.Equal(ChannelId, harness.Reply.ConversationId);
        Assert.Equal("root-1", harness.Reply.ActivityId);
        Assert.Equal(1, harness.Reply.Calls);
        Assert.Equal(0, harness.Send.Calls);
    }

    [Fact]
    public async Task Send_ChannelThreadReply_RepliesInTheReplyChainOfTheThread()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(InboundInChannelThread());

        await context.SendAsync("hi");

        Assert.Equal(ChannelId, harness.Reply.ConversationId);
        Assert.Equal("root-1", harness.Reply.ActivityId);
        Assert.Equal(1, harness.Reply.Calls);
        Assert.Equal(0, harness.Send.Calls);
    }

    [Fact]
    public async Task Send_ChannelThreadReplyWithReplyChainButNoChannelId_SendsToInboundConversationUnchanged()
    {
        Harness harness = CreateHarness();
        string inboundConversationId = $"{ChannelId};messageid=root-1";
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: inboundConversationId,
            id: "reply-2",
            channelData: new { chat = new { id = ChatId }, replyChain = new { id = "root-1" } },
            conversationType: "channel"));

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

    [Fact]
    public async Task Send_PersonalInboundInsideReplyChain_RepliesInThatChain()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: OpaqueConversationId,
            id: "reply-2",
            channelData: new { chat = new { id = ChatId }, replyChain = new { id = "root-1" } },
            conversationType: "personal"));

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

    [Fact]
    public async Task Send_ChannelReplyChainRouteNotFound_SendsToInboundConversationInstead()
    {
        Harness harness = CreateHarness(HttpStatusCode.NotFound);
        Context<MessageActivity> context = harness.ContextFor(InboundInChannelThread());

        SendActivityResponse? response = await context.SendAsync("hi");

        Assert.Equal("sent-id", response?.Id);
        Assert.Equal(1, harness.Reply.Calls);
        Assert.Equal(ChannelId, harness.Reply.ConversationId);
        Assert.Equal(1, harness.Send.Calls);
        Assert.Equal(ChannelThreadConversationId, harness.Send.ConversationId);
        Assert.False(harness.Send.IsTargeted);
    }

    [Fact]
    public async Task Send_PersonalReplyChainRouteNotFound_PropagatesWithoutSendingToTheInboundConversation()
    {
        Harness harness = CreateHarness(HttpStatusCode.NotFound);
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: OpaqueConversationId,
            id: "reply-2",
            channelData: new { chat = new { id = ChatId }, replyChain = new { id = "root-1" } },
            conversationType: "personal"));

        HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => context.SendAsync("hi"));

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
        Assert.Equal(1, harness.Reply.Calls);
        Assert.Equal(0, harness.Send.Calls);
    }

    [Fact]
    public async Task Send_ChannelReplyChainRouteNotFound_KeepsATargetedMessageTargeted()
    {
        Harness harness = CreateHarness(HttpStatusCode.NotFound);
        Context<MessageActivity> context = harness.ContextFor(InboundInChannelThread());

        await context.SendAsync(new MessageActivityInput()
            .WithText("only you")
            .WithRecipient(new TeamsChannelAccount { Id = "user-1" }, isTargeted: true));

        Assert.True(harness.Reply.IsTargeted);
        Assert.Equal(ChannelThreadConversationId, harness.Send.ConversationId);
        Assert.True(harness.Send.IsTargeted);
    }

    [Fact]
    public async Task Reply_ChannelReplyChainRouteNotFound_SendsTheQuotedMessageToTheInboundConversation()
    {
        Harness harness = CreateHarness(HttpStatusCode.NotFound);
        Context<MessageActivity> context = harness.ContextFor(InboundInChannelThread());

        await context.ReplyAsync("hi");

        Assert.Equal(ChannelThreadConversationId, harness.Send.ConversationId);
        Assert.Contains("quotedReply", harness.Send.Activity!.ToJson(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Send_ChannelReplyChainCallFailsWithAnotherError_PropagatesWithoutFallingBack(HttpStatusCode statusCode)
    {
        Harness harness = CreateHarness(statusCode);
        Context<MessageActivity> context = harness.ContextFor(InboundInChannelThread());

        HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => context.SendAsync("hi"));

        Assert.Equal(statusCode, ex.StatusCode);
        Assert.Equal(0, harness.Send.Calls);
    }

    [Fact]
    public async Task Send_ChannelReplyChainCallFailsWithANonHttpError_PropagatesWithoutFallingBack()
    {
        Harness harness = CreateHarness(replyFailure: new InvalidOperationException("boom"));
        Context<MessageActivity> context = harness.ContextFor(InboundInChannelThread());

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SendAsync("hi"));

        Assert.Equal(0, harness.Send.Calls);
    }

    [Fact]
    public async Task Send_ChannelFallbackAfterNotFoundFails_PropagatesTheFallbackError()
    {
        Harness harness = CreateHarness(HttpStatusCode.NotFound, sendFailure: new HttpRequestException("boom", null, HttpStatusCode.Forbidden));
        Context<MessageActivity> context = harness.ContextFor(InboundInChannelThread());

        HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => context.SendAsync("hi"));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }

    [Fact]
    public async Task CreateFromContext_ChannelInboundInsideThread_StartsTheStreamInThatChain()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(InboundInChannelThread());
        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);

        await writer.AppendResponseAsync("hello");

        Assert.Equal(1, harness.Reply.Calls);
        Assert.Equal(ChannelId, harness.Reply.ConversationId);
        Assert.Equal("root-1", harness.Reply.ActivityId);
        Assert.Equal(0, harness.Send.Calls);
    }

    [Fact]
    public async Task CreateFromContext_ChannelRootPost_StartsTheStreamInThatChain()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: ChannelThreadConversationId,
            id: "root-1",
            channelData: new { channel = new { id = ChannelId }, replyChain = new { id = "root-1" } },
            conversationType: "channel"));
        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);

        await writer.AppendResponseAsync("hello");

        Assert.Equal(1, harness.Reply.Calls);
        Assert.Equal(ChannelId, harness.Reply.ConversationId);
        Assert.Equal("root-1", harness.Reply.ActivityId);
        Assert.Equal(0, harness.Send.Calls);
    }

    [Fact]
    public async Task CreateFromContext_InboundInsideReplyChain_StartsTheStreamInThatChain()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(InboundInThread());
        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);

        await writer.AppendResponseAsync("hello");

        Assert.Equal(1, harness.Reply.Calls);
        Assert.Equal(ChatId, harness.Reply.ConversationId);
        Assert.Equal("root-1", harness.Reply.ActivityId);
        Assert.Equal(0, harness.Send.Calls);
    }

    [Fact]
    public async Task CreateFromContext_InboundInsideReplyChain_StartsTheStreamAtTheInboundServiceUrl()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(InboundInThread());
        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);

        await writer.AppendResponseAsync("hello");

        Assert.Equal(1, harness.Reply.Calls);
        Assert.Equal(Harness.ServiceUrl, harness.Reply.ServiceUrl);
    }

    [Fact]
    public async Task CreateFromContext_InboundInsideReplyChain_OnlyTheFirstChunkUsesTheChain()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(InboundInThread());
        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);

        await writer.AppendResponseAsync("hello");
        await writer.FinalizeResponseAsync();

        Assert.Equal(1, harness.Reply.Calls);
        Assert.Equal(1, harness.Send.Calls);
        Assert.Equal(OpaqueConversationId, harness.Send.ConversationId);
        Assert.Contains("\"streamType\": \"final\"", harness.Send.Activity!.ToJson(), StringComparison.Ordinal);
        Assert.Contains("\"streamId\": \"reply-id\"", harness.Send.Activity!.ToJson(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateFromContext_FirstChunkTimesOut_FinalizeStartsTheMessageInTheReplyChain()
    {
        Harness harness = CreateHarness(replyFailureByCall: call => call == 1
            ? new HttpRequestException("Forbidden: exceeded streaming time", null, HttpStatusCode.Forbidden)
            : null);
        Context<MessageActivity> context = harness.ContextFor(InboundInThread());
        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);

        await writer.AppendResponseAsync("hello");
        Assert.True(writer.TimedOut);
        await writer.FinalizeResponseAsync();

        Assert.Equal(2, harness.Reply.Calls);
        Assert.Equal(ChatId, harness.Reply.ConversationId);
        Assert.Equal("root-1", harness.Reply.ActivityId);
        Assert.Equal(0, harness.Send.Calls);
    }

    [Fact]
    public async Task CreateFromContext_WriterLogsThroughTheBotApplicationLogger()
    {
        Mock<ILogger<TeamsBotApplication>> logger = new();
        logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        Harness harness = CreateHarness(logger: logger.Object);
        Context<MessageActivity> context = harness.ContextFor(InboundInThread());
        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);

        await writer.AppendResponseAsync("hello");
        await writer.FinalizeResponseAsync();

        logger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Finalizing stream", StringComparison.Ordinal)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateFromContext_InboundTopLevelMessage_StartsTheStreamInTheConversation()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: OpaqueConversationId,
            id: "root-1",
            channelData: new { chat = new { id = ChatId }, replyChain = new { id = "root-1" } }));
        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);

        await writer.AppendResponseAsync("hello");

        Assert.Equal(0, harness.Reply.Calls);
        Assert.Equal(OpaqueConversationId, harness.Send.ConversationId);
    }

    [Fact]
    public async Task CreateFromContext_GroupChatInboundInsideReplyChain_StartsTheStreamInTheConversation()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: GroupChatId,
            id: "reply-2",
            channelData: new { chat = new { id = GroupChatId }, replyChain = new { id = "root-1" } },
            conversationType: "groupChat"));
        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);

        await writer.AppendResponseAsync("hello");

        Assert.Equal(0, harness.Reply.Calls);
        Assert.Equal(GroupChatId, harness.Send.ConversationId);
    }

    [Fact]
    public async Task CreateFromContext_ReplyChainRouteNotFound_PropagatesWithoutStartingTheStreamInTheConversation()
    {
        Harness harness = CreateHarness(HttpStatusCode.NotFound);
        Context<MessageActivity> context = harness.ContextFor(InboundInThread());
        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);

        HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => writer.AppendResponseAsync("hello"));

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
        Assert.Equal(1, harness.Reply.Calls);
        Assert.Equal(0, harness.Send.Calls);
    }

    private static MessageActivity InboundInChannelThread() => Inbound(
        conversationId: ChannelThreadConversationId,
        id: "reply-2",
        channelData: new { channel = new { id = ChannelId }, replyChain = new { id = "root-1" } },
        conversationType: "channel");

    private static MessageActivity InboundInThread() => Inbound(
        conversationId: OpaqueConversationId,
        id: "reply-2",
        channelData: new { chat = new { id = ChatId }, replyChain = new { id = "root-1" } });

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
        public Uri? ServiceUrl { get; set; }
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

    private static Harness CreateHarness(HttpStatusCode? replyFailureStatus = null, Exception? replyFailure = null, Exception? sendFailure = null, Func<int, Exception?>? replyFailureByCall = null, ILogger<TeamsBotApplication>? logger = null)
    {
        if (replyFailureStatus is { } status)
        {
            replyFailure = new HttpRequestException($"Error replying to activity {status}.", null, status);
        }

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
            .Returns(() => sendFailure is null
                ? Task.FromResult<SendActivityResponse?>(new SendActivityResponse { Id = "sent-id" })
                : Task.FromException<SendActivityResponse?>(sendFailure));

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
                (conversationId, activityId, activity, serviceUrl, isTargeted, _, _, _) =>
                {
                    reply.Calls++;
                    reply.ConversationId = conversationId;
                    reply.ActivityId = activityId;
                    reply.ServiceUrl = serviceUrl;
                    reply.Activity = activity;
                    reply.IsTargeted = isTargeted;
                })
            .Returns(() =>
            {
                Exception? failure = replyFailureByCall?.Invoke(reply.Calls) ?? replyFailure;
                return failure is null
                    ? Task.FromResult<SendActivityResponse?>(new SendActivityResponse { Id = "reply-id" })
                    : Task.FromException<SendActivityResponse?>(failure);
            });

        TeamsBotApplication app = new(
            new ApiClient(new HttpClient(), conversationClient.Object, userTokenClient.Object),
            new HttpContextAccessor(),
            logger ?? NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "test-app-id" });

        return new Harness { App = app, Send = send, Reply = reply };
    }
}
