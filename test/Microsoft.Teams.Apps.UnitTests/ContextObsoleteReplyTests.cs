// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

#pragma warning disable CS0618 // the obsolete inbound-activity overloads are the subject of these tests

public class ContextObsoleteReplyTests
{
    [Fact]
    public async Task Reply_InboundActivityOverload_SendsToTheInboundConversationWithoutReplyToId()
    {
        (Context<MessageActivity> context, _, SendCapture capture) = CreateContext();
        MessageActivity outgoing = new("hello");

        await context.Reply(outgoing);

        Assert.Null(ReplyToId(capture.Activity));
        Assert.Equal("conv-1", capture.ConversationId);
    }

    [Fact]
    public async Task Quote_InboundActivityOverload_SendsToTheInboundConversationWithoutReplyToId()
    {
        (Context<MessageActivity> context, _, SendCapture capture) = CreateContext();

        await context.Quote("quoted-1", new MessageActivity("hello"));

        Assert.Null(ReplyToId(capture.Activity));
        Assert.Equal("conv-1", capture.ConversationId);
    }

    [Fact]
    public async Task Send_InboundActivityOverload_SendsWithoutReplyToId()
    {
        (Context<MessageActivity> context, _, SendCapture capture) = CreateContext();

        await context.Send(new MessageActivity("hello"));

        Assert.Null(ReplyToId(capture.Activity));
    }

    [Theory]
    [InlineData("Send")]
    [InlineData("Reply")]
    [InlineData("Quote")]
    public async Task InboundActivityOverloads_InsideAReplyChain_PostToTheInboundConversation(string method)
    {
        (Context<MessageActivity> context, Mock<ConversationClient> conversationClient, SendCapture capture) = CreateContext(insideReplyChain: true);
        MessageActivity outgoing = new("hello");

        Task<SendActivityResponse?> sent = method switch
        {
            "Send" => context.Send(outgoing),
            "Reply" => context.Reply(outgoing),
            _ => context.Quote("quoted-1", outgoing),
        };
        await sent;

        Assert.Equal("conv-1", capture.ConversationId);
        VerifyNoReplyChainCall(conversationClient);
    }

    private static string? ReplyToId(CoreActivityInput? activity)
    {
        using JsonDocument json = JsonDocument.Parse(activity!.ToJson());
        return json.RootElement.TryGetProperty("replyToId", out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private sealed class SendCapture
    {
        public string? ConversationId { get; set; }
        public CoreActivityInput? Activity { get; set; }
    }

    private static void VerifyNoReplyChainCall(Mock<ConversationClient> conversationClient)
    {
        conversationClient.Verify(
            c => c.SendReplyChainActivityAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CoreActivityInput>(),
                It.IsAny<Uri>(),
                It.IsAny<bool>(),
                It.IsAny<BotRequestContext?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static (Context<MessageActivity> Context, Mock<ConversationClient> ConversationClient, SendCapture Capture) CreateContext(string? inboundId = "inbound-1", bool insideReplyChain = false)
    {
        Mock<UserTokenClient> userTokenClient = new(
            new HttpClient(),
            new Mock<IConfiguration>().Object,
            NullLogger<UserTokenClient>.Instance);
        Mock<ConversationClient> conversationClient = new(new HttpClient(), NullLogger<ConversationClient>.Instance);
        SendCapture capture = new();
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
                (conversationId, activity, _, _, _, _, _) =>
                {
                    capture.ConversationId = conversationId;
                    capture.Activity = activity;
                })
            .ReturnsAsync(new SendActivityResponse { Id = "sent-id" });

        TeamsBotApplication app = new(
            new ApiClient(new HttpClient(), conversationClient.Object, userTokenClient.Object),
            new HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "test-app-id" });

        MessageActivity inbound = new("inbound text")
        {
            Id = inboundId,
            ChannelId = "msteams",
            ServiceUrl = new Uri("https://smba.trafficmanager.net/test/"),
            From = new TeamsChannelAccount { Id = "user-1", Name = "User" },
            Recipient = new TeamsChannelAccount { Id = "bot-1", Name = "Bot" },
            Conversation = new TeamsConversation { Id = "conv-1" },
            ChannelData = insideReplyChain
                ? JsonSerializer.Deserialize<TeamsChannelData>("{\"chat\":{\"id\":\"19:chat@unq.gbl.spaces\"},\"replyChain\":{\"id\":\"root-1\"}}")
                : null,
        };

        return (new Context<MessageActivity>(app, inbound), conversationClient, capture);
    }
}
