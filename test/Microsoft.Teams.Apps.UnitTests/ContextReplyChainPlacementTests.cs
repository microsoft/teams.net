// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.MessageExtensions;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Apps.TaskModules;
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

    [Theory]
    [InlineData("card-message")]
    [InlineData("card-invoke")]
    [InlineData("message-action")]
    [InlineData("reaction")]
    [InlineData("edit")]
    [InlineData("delete")]
    public async Task Send_ShapeOnAMessageInsideAThread_RepliesInThatChain(string shape)
    {
        Harness harness = CreateHarness();
        Context<TeamsActivity> context = new(harness.App, Shape(shape, messageId: "105", replyChainId: "100"));

        await context.SendAsync("hi");

        Assert.Equal(1, harness.Reply.Calls);
        Assert.Equal(ChatId, harness.Reply.ConversationId);
        Assert.Equal("100", harness.Reply.ActivityId);
        Assert.Equal(0, harness.Send.Calls);
    }

    [Theory]
    [InlineData("card-message")]
    [InlineData("card-invoke")]
    [InlineData("message-action")]
    [InlineData("reaction")]
    [InlineData("edit")]
    [InlineData("delete")]
    public async Task Send_ShapeOnATopLevelMessage_SendsToInboundConversation(string shape)
    {
        Harness harness = CreateHarness();
        Context<TeamsActivity> context = new(harness.App, Shape(shape, messageId: "100", replyChainId: "100"));

        await context.SendAsync("hi");

        Assert.Equal(OpaqueConversationId, harness.Send.ConversationId);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Fact]
    public async Task Send_MessageActionInsideAThread_ComparesTheMessagePayloadIdNotTheReplyId()
    {
        Harness harness = CreateHarness();
        Context<TeamsActivity> context = new(harness.App, Shape("message-action", messageId: "105", replyChainId: "100", replyToId: "100"));

        await context.SendAsync("hi");

        Assert.Equal(1, harness.Reply.Calls);
        Assert.Equal("100", harness.Reply.ActivityId);
    }

    [Theory]
    [InlineData("card-message", "1:abc")]
    [InlineData("card-message", "0")]
    [InlineData("card-message", "12a")]
    [InlineData("card-invoke", "1:abc")]
    [InlineData("card-invoke", "0")]
    [InlineData("card-invoke", "12a")]
    [InlineData("card-message", "")]
    [InlineData("card-invoke", "")]
    [InlineData("card-message", "١٠٥")]
    [InlineData("card-invoke", "١٠٥")]
    public async Task Send_CardShapeWithAReplyToIdThatIsNotAPlainNumber_SendsToInboundConversation(string shape, string replyToId)
    {
        Harness harness = CreateHarness();
        Context<TeamsActivity> context = new(harness.App, Shape(shape, messageId: replyToId, replyChainId: "100"));

        await context.SendAsync("hi");

        Assert.Equal(OpaqueConversationId, harness.Send.ConversationId);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Fact]
    public async Task Send_CardMessageWithoutAReplyToId_SendsToInboundConversation()
    {
        Harness harness = CreateHarness();
        Context<TeamsActivity> context = new(harness.App, Shape("card-message", messageId: null, replyChainId: "100"));

        await context.SendAsync("hi");

        Assert.Equal(OpaqueConversationId, harness.Send.ConversationId);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Fact]
    public async Task Send_InvokeNotTiedToAMessageWithAReplyChainId_RepliesInThatChain()
    {
        Harness harness = CreateHarness();
        Context<TeamsActivity> context = new(harness.App, Shape("contextless-invoke", messageId: null, replyChainId: "100"));

        await context.SendAsync("hi");

        Assert.Equal(1, harness.Reply.Calls);
        Assert.Equal(ChatId, harness.Reply.ConversationId);
        Assert.Equal("100", harness.Reply.ActivityId);
    }

    [Fact]
    public async Task Send_InvokeNotTiedToAMessageWithoutAReplyChainId_SendsToInboundConversation()
    {
        Harness harness = CreateHarness();
        Context<TeamsActivity> context = new(harness.App, Shape("contextless-invoke", messageId: null, replyChainId: null));

        await context.SendAsync("hi");

        Assert.Equal(OpaqueConversationId, harness.Send.ConversationId);
        Assert.Equal(0, harness.Reply.Calls);
    }

    // Rows: shape, message id, reply chain id, replyToId, channelData.source as JSON (null means omitted), expected to be placed in the reply chain.
    // channelData.source is absent when omitted, null or an empty object.
    // An empty messagePayload.id is never inside a thread and does not fall through to replyToId.
    // A null replyToId on an invoke is absent.
    [Theory]
    [InlineData("typed-message", "105", "100", null, null, true)]
    [InlineData("typed-message", "105", "100", null, "null", true)]
    [InlineData("typed-message", "105", "100", null, "{}", true)]
    [InlineData("typed-message", "105", "100", "100", null, true)]
    [InlineData("typed-message", "105", "100", "100", "null", true)]
    [InlineData("typed-message", "105", "100", "100", "{}", true)]
    [InlineData("typed-message", "105", "100", "100", "{\"name\":\"message\"}", false)]
    [InlineData("typed-message", "105", "100", "105", "{}", true)]
    [InlineData("typed-message", "105", "100", "105", "{\"name\":\"message\"}", true)]
    [InlineData("contextless-invoke", null, "100", null, null, true)]
    [InlineData("contextless-invoke", null, null, null, null, false)]
    [InlineData("card-invoke", "105", "100", null, null, true)]
    [InlineData("card-invoke", "100", "100", null, null, false)]
    [InlineData("message-action", "", "100", null, null, false)]
    [InlineData("message-action", "", "100", "105", null, false)]
    [InlineData("message-action", null, "100", "105", null, true)]
    [InlineData("message-action", null, "100", "100", null, false)]
    [InlineData("message-action", null, "100", null, null, true)]
    [InlineData("typed-message", "100", "100", null, "null", false)]
    [InlineData("typed-message", "100", "100", null, "{}", false)]
    [InlineData("typed-message", "100", "100", "105", "{}", false)]
    [InlineData("typed-message", "105", "100", "100", "{\"name\":\"\"}", true)]
    [InlineData("card-invoke", "", "100", null, null, false)]
    [InlineData("message-action", "100", "100", null, null, false)]
    [InlineData("message-action", "105", "100", "100", null, true)]
    public async Task Send_ParsedPayloadNullAndEmptyFields_AreClassifiedAsDocumented(string shape, string? messageId, string? replyChainId, string? replyToId, string? sourceJson, bool placed)
    {
        Harness harness = CreateHarness();
        Context<TeamsActivity> context = new(harness.App, Shape(shape, messageId, replyChainId, replyToId, sourceJson: sourceJson));

        await context.SendAsync("hi");

        Assert.Equal(placed ? 1 : 0, harness.Reply.Calls);
        Assert.Equal(placed ? 0 : 1, harness.Send.Calls);
        if (placed)
        {
            Assert.Equal(ChatId, harness.Reply.ConversationId);
            Assert.Equal(replyChainId, harness.Reply.ActivityId);
        }
        else
        {
            Assert.Equal(OpaqueConversationId, harness.Send.ConversationId);
        }
    }

    [Theory]
    [InlineData("typed-message", "105", "100", null, null, true)]
    [InlineData("typed-message", "105", "100", null, "null", true)]
    [InlineData("typed-message", "105", "100", null, "{}", true)]
    [InlineData("typed-message", "105", "100", "100", null, true)]
    [InlineData("typed-message", "105", "100", "100", "null", true)]
    [InlineData("typed-message", "105", "100", "100", "{}", true)]
    [InlineData("typed-message", "105", "100", "100", "{\"name\":\"message\"}", false)]
    [InlineData("typed-message", "105", "100", "105", "{}", true)]
    [InlineData("typed-message", "105", "100", "105", "{\"name\":\"message\"}", true)]
    [InlineData("contextless-invoke", null, "100", null, null, true)]
    [InlineData("contextless-invoke", null, null, null, null, false)]
    [InlineData("card-invoke", "105", "100", null, null, true)]
    [InlineData("card-invoke", "100", "100", null, null, false)]
    [InlineData("message-action", "", "100", null, null, false)]
    [InlineData("message-action", "", "100", "105", null, false)]
    [InlineData("message-action", null, "100", "105", null, true)]
    [InlineData("message-action", null, "100", "100", null, false)]
    [InlineData("message-action", null, "100", null, null, true)]
    [InlineData("typed-message", "100", "100", null, "null", false)]
    [InlineData("typed-message", "100", "100", null, "{}", false)]
    [InlineData("typed-message", "100", "100", "105", "{}", false)]
    [InlineData("typed-message", "105", "100", "100", "{\"name\":\"\"}", true)]
    [InlineData("card-invoke", "", "100", null, null, false)]
    [InlineData("message-action", "100", "100", null, null, false)]
    [InlineData("message-action", "105", "100", "100", null, true)]
    public async Task CreateFromContext_ParsedPayloadNullAndEmptyFields_AreClassifiedLikeSend(string shape, string? messageId, string? replyChainId, string? replyToId, string? sourceJson, bool placed)
    {
        Harness harness = CreateHarness();
        Context<TeamsActivity> context = new(harness.App, Shape(shape, messageId, replyChainId, replyToId, sourceJson: sourceJson));
        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);

        await writer.AppendResponseAsync("hello");

        Assert.Equal(placed ? 1 : 0, harness.Reply.Calls);
        Assert.Equal(placed ? 0 : 1, harness.Send.Calls);
        if (placed)
        {
            Assert.Equal(ChatId, harness.Reply.ConversationId);
            Assert.Equal(replyChainId, harness.Reply.ActivityId);
        }
        else
        {
            Assert.Equal(OpaqueConversationId, harness.Send.ConversationId);
        }
    }

    // value.messagePayload that is not an object, or whose id is not a string, falls back to replyToId and never throws.
    [Theory]
    [InlineData("\"text\"", "105", true)]
    [InlineData("5", "105", true)]
    [InlineData("true", "105", true)]
    [InlineData("[1, 2]", "105", true)]
    [InlineData("null", "105", true)]
    [InlineData("\"text\"", "100", false)]
    [InlineData("{\"id\":100}", "100", false)]
    [InlineData("{\"id\":105}", "100", false)]
    public async Task Send_InvokeWithAMessagePayloadThatIsNotAnObjectWithAStringId_FallsBackToReplyToId(string messagePayloadJson, string replyToId, bool placed)
    {
        Harness harness = CreateHarness();
        Context<TeamsActivity> context = new(harness.App, Shape("select-item", messagePayloadJson, "100", replyToId));

        await context.SendAsync("hi");

        Assert.Equal(placed ? 1 : 0, harness.Reply.Calls);
        Assert.Equal(placed ? 0 : 1, harness.Send.Calls);
    }

    [Theory]
    [InlineData("\"text\"", "105", true)]
    [InlineData("5", "105", true)]
    [InlineData("true", "105", true)]
    [InlineData("[1, 2]", "105", true)]
    [InlineData("null", "105", true)]
    [InlineData("\"text\"", "100", false)]
    [InlineData("{\"id\":100}", "100", false)]
    [InlineData("{\"id\":105}", "100", false)]
    public async Task CreateFromContext_InvokeWithAMessagePayloadThatIsNotAnObjectWithAStringId_FallsBackToReplyToId(string messagePayloadJson, string replyToId, bool placed)
    {
        Harness harness = CreateHarness();
        Context<TeamsActivity> context = new(harness.App, Shape("select-item", messagePayloadJson, "100", replyToId));
        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);

        await writer.AppendResponseAsync("hello");

        Assert.Equal(placed ? 1 : 0, harness.Reply.Calls);
        Assert.Equal(placed ? 0 : 1, harness.Send.Calls);
    }

    [Theory]
    [InlineData("conversationUpdate")]
    [InlineData("installationUpdate")]
    [InlineData("event")]
    public async Task Send_OtherInboundActivity_SendsToInboundConversation(string type)
    {
        Harness harness = CreateHarness();
        Context<TeamsActivity> context = new(harness.App, Shape("other", messageId: "105", replyChainId: "100", type: type));

        await context.SendAsync("hi");

        Assert.Equal(OpaqueConversationId, harness.Send.ConversationId);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Theory]
    [InlineData("card-message")]
    [InlineData("card-invoke")]
    [InlineData("reaction")]
    public async Task Send_ShapeInAGroupChat_SendsToInboundConversation(string shape)
    {
        Harness harness = CreateHarness();
        Context<TeamsActivity> context = new(harness.App, Shape(shape, messageId: "105", replyChainId: "100", conversationType: "groupChat"));

        await context.SendAsync("hi");

        Assert.Equal(OpaqueConversationId, harness.Send.ConversationId);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Theory]
    [InlineData("card-invoke")]
    [InlineData("reaction")]
    public async Task Send_InvokeOrReactionInAChannel_SendsToInboundConversation(string shape)
    {
        Harness harness = CreateHarness();
        Context<TeamsActivity> context = new(harness.App, Shape(shape, messageId: "105", replyChainId: "100", conversationType: "channel"));

        await context.SendAsync("hi");

        Assert.Equal(ChannelThreadConversationId, harness.Send.ConversationId);
        Assert.Equal(0, harness.Reply.Calls);
    }

    [Theory]
    [InlineData("card-invoke", "105", "100", true)]
    [InlineData("card-invoke", "100", "100", false)]
    [InlineData("message-action", "105", "100", true)]
    [InlineData("message-action", "100", "100", false)]
    [InlineData("contextless-invoke", null, "100", true)]
    [InlineData("contextless-invoke", null, null, false)]
    public async Task Send_FromATypedInvokeHandler_IsPlacedPerTheInboundShape(string shape, string? messageId, string? replyChainId, bool placed)
    {
        Harness harness = CreateHarness();
        TeamsBotApplication app = harness.App;

        switch (shape)
        {
            case "card-invoke":
                app.OnAdaptiveCardAction(async (ctx, ct) =>
                {
                    await ctx.SendAsync("hi", ct);
                    return new InvokeResponse(200);
                });
                break;
            case "message-action":
                app.OnSubmitAction(async (ctx, ct) =>
                {
                    await ctx.SendAsync("hi", ct);
                    return new InvokeResponse<MessageExtensionActionResponse>(200);
                });
                break;
            default:
                app.OnTaskFetch(async (ctx, ct) =>
                {
                    await ctx.SendAsync("hi", ct);
                    return new InvokeResponse<TaskModuleResponse>(200);
                });
                break;
        }

        await app.Router.DispatchWithReturnAsync(new Context<TeamsActivity>(app, Shape(shape, messageId, replyChainId)));

        Assert.Equal(placed ? 1 : 0, harness.Reply.Calls);
        Assert.Equal(placed ? 0 : 1, harness.Send.Calls);
        if (placed)
        {
            Assert.Equal(ChatId, harness.Reply.ConversationId);
            Assert.Equal(replyChainId, harness.Reply.ActivityId);
        }
        else
        {
            Assert.Equal(OpaqueConversationId, harness.Send.ConversationId);
        }
    }

    [Fact]
    public async Task Send_CardMessageInAChannel_RepliesInTheReplyChainOfTheThread()
    {
        Harness harness = CreateHarness();
        Context<TeamsActivity> context = new(harness.App, Shape("card-message", messageId: "105", replyChainId: "100", conversationType: "channel"));

        await context.SendAsync("hi");

        Assert.Equal(1, harness.Reply.Calls);
        Assert.Equal(ChannelId, harness.Reply.ConversationId);
        Assert.Equal("100", harness.Reply.ActivityId);
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

    [Theory]
    [InlineData("{ \"chat\": { \"id\": \"" + ChatId + "\" } }")]
    [InlineData("{ \"replyChain\": { \"id\": \"root-1\" } }")]
    [InlineData("{ \"chat\": { \"id\": \" \" }, \"replyChain\": { \"id\": \"root-1\" } }")]
    [InlineData("{ \"chat\": { \"id\": \"" + ChatId + "\" }, \"replyChain\": { \"id\": \" \" } }")]
    [InlineData("{}")]
    public async Task CreateFromContext_PayloadMissingAnIdOrWithABlankOne_StartsTheStreamInTheInboundConversation(string channelDataJson)
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: OpaqueConversationId,
            id: "reply-2",
            channelData: JsonDocument.Parse(channelDataJson).RootElement));
        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);

        await writer.AppendResponseAsync("hello");

        Assert.Equal(0, harness.Reply.Calls);
        Assert.Equal(OpaqueConversationId, harness.Send.ConversationId);
    }

    [Fact]
    public async Task CreateFromContext_ChannelWithABlankChannelId_StartsTheStreamInTheInboundConversation()
    {
        Harness harness = CreateHarness();
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: ChannelThreadConversationId,
            id: "reply-2",
            channelData: new { channel = new { id = " " }, replyChain = new { id = "root-1" } },
            conversationType: "channel"));
        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);

        await writer.AppendResponseAsync("hello");

        Assert.Equal(0, harness.Reply.Calls);
        Assert.Equal(ChannelThreadConversationId, harness.Send.ConversationId);
    }

    [Fact]
    public async Task CreateFromContext_ChannelReplyChainRouteNotFound_StartsTheStreamInTheInboundConversation()
    {
        Harness harness = CreateHarness(HttpStatusCode.NotFound);
        Context<MessageActivity> context = harness.ContextFor(Inbound(
            conversationId: ChannelThreadConversationId,
            id: "reply-2",
            channelData: new { channel = new { id = ChannelId }, replyChain = new { id = "1680000000000" } },
            conversationType: "channel"));
        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);

        await writer.AppendResponseAsync("hello");

        Assert.Equal(1, harness.Reply.Calls);
        Assert.Equal(1, harness.Send.Calls);
        Assert.Equal(ChannelThreadConversationId, harness.Send.ConversationId);
    }

    [Fact]
    public async Task CreateFromContext_ChannelReplyChainRouteNotFoundForANonNumericId_Propagates()
    {
        Harness harness = CreateHarness(HttpStatusCode.NotFound);
        Context<MessageActivity> context = harness.ContextFor(InboundInChannelThread());
        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);

        HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => writer.AppendResponseAsync("hello"));

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
        Assert.Equal(0, harness.Send.Calls);
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

    private static TeamsActivity Shape(string shape, string? messageId, string? replyChainId, string? replyToId = null, string? type = null, string? conversationType = null, string? sourceJson = null)
    {
        bool inChannel = conversationType == "channel";
        Dictionary<string, object> channelData = new();
        if (inChannel)
        {
            channelData["channel"] = new { id = ChannelId };
        }
        else
        {
            channelData["chat"] = new { id = ChatId };
        }

        if (replyChainId is not null)
        {
            channelData["replyChain"] = new { id = replyChainId };
        }

        string activityType = "message";
        string id = "f:client-id";
        object? value = null;
        string? invokeName = null;
        switch (shape)
        {
            case "typed-message":
                id = messageId!;
                break;
            case "card-message":
                channelData["source"] = new { name = "message" };
                replyToId ??= messageId;
                break;
            case "card-invoke":
                activityType = "invoke";
                invokeName = "adaptiveCard/action";
                replyToId ??= messageId;
                break;
            case "message-action":
                activityType = "invoke";
                invokeName = "composeExtension/submitAction";
                value = new { messagePayload = new { id = messageId } };
                break;
            case "select-item":
                // messageId carries the raw JSON of value.messagePayload.
                activityType = "invoke";
                invokeName = "composeExtension/selectItem";
                value = JsonDocument.Parse("{\"messagePayload\":" + messageId + "}").RootElement;
                break;
            case "contextless-invoke":
                activityType = "invoke";
                invokeName = "task/fetch";
                break;
            case "reaction":
                activityType = "messageReaction";
                id = messageId!;
                break;
            case "edit":
                activityType = "messageUpdate";
                id = messageId!;
                break;
            case "delete":
                activityType = "messageDelete";
                id = messageId!;
                break;
            case "other":
                activityType = type!;
                id = messageId!;
                break;
        }

        if (sourceJson is not null)
        {
            channelData["source"] = JsonDocument.Parse(sourceJson).RootElement;
        }

        string conversationId = inChannel ? ChannelThreadConversationId : OpaqueConversationId;
        string json = JsonSerializer.Serialize(new
        {
            type = activityType,
            id,
            name = invokeName,
            replyToId,
            value,
            channelId = "msteams",
            serviceUrl = Harness.ServiceUrl.ToString(),
            from = new { id = "user-1", name = "User" },
            recipient = new { id = "bot-1", name = "Bot" },
            conversation = conversationType is null ? new { id = conversationId, conversationType = (string?)null } : new { id = conversationId, conversationType = (string?)conversationType },
            channelData,
        });

        return TeamsActivity.FromActivity(CoreActivity.FromJsonString(json));
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
