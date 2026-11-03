// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

/// <summary>
/// Unit tests for the quote guard in <see cref="Context{TActivity}.ReplyAsync(string, CancellationToken)"/>.
/// </summary>
public class ReplyQuoteGuardTests
{
    private sealed record Sent(string Method, string Path, JsonNode Body);

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<Sent> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string body = request.Content is not null
                ? await request.Content.ReadAsStringAsync(cancellationToken)
                : "{}";
            Requests.Add(new Sent(request.Method.Method, request.RequestUri!.AbsolutePath, JsonNode.Parse(body)!));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"id\":\"sent-id\"}") };
        }
    }

    private static CoreActivity Inbound(string type, string? id) => new(type)
    {
        Id = id,
        ServiceUrl = new Uri("https://smba.trafficmanager.net/amer/"),
        ChannelId = "msteams",
        Conversation = new Conversation { Id = "19:conv@thread.v2" },
        From = new ChannelAccount { Id = "29:user", Name = "User" },
        Recipient = new ChannelAccount { Id = "28:bot", Name = "Bot" },
    };

    private static (Context<TeamsActivity> context, RecordingHandler handler) CreateContext(TeamsActivity activity)
        => CreateTypedContext(activity);

    private static (Context<T> context, RecordingHandler handler) CreateTypedContext<T>(T activity) where T : TeamsActivity
    {
        RecordingHandler handler = new();
        ConversationClient conversationClient = new(new HttpClient(handler), NullLogger<ConversationClient>.Instance);
        Mock<UserTokenClient> userTokenClient = new(
            new HttpClient(),
            new Mock<IConfiguration>().Object,
            NullLogger<UserTokenClient>.Instance);
        ApiClient apiClient = new(new HttpClient(), conversationClient, userTokenClient.Object);
        TeamsBotApplication app = new(
            apiClient,
            new HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "test-app-id" });
        return (new Context<T>(app, activity), handler);
    }

    private static bool HasQuote(Sent sent)
    {
        bool hasEntity = sent.Body["entities"] is JsonArray entities
            && entities.Any(e => e?["type"]?.GetValue<string>() == "quotedReply");
        bool hasPlaceholder = sent.Body["text"]?.GetValue<string>()?.Contains("<quoted", StringComparison.Ordinal) == true;
        return hasEntity || hasPlaceholder;
    }

    [Fact]
    public async Task ReplyAsync_SyntheticGuidId_SendsWithoutQuote()
    {
        (Context<TeamsActivity> context, RecordingHandler handler) = CreateContext(
            MessageActivity.FromActivity(Inbound(TeamsActivityTypes.Message, "f:5d1a8c9e-3c77-4b1f-9c52-0a8f5f7a2d11")));

        await context.ReplyAsync("hello");

        Sent sent = Assert.Single(handler.Requests);
        Assert.False(HasQuote(sent));
        Assert.Equal("hello", sent.Body["text"]?.GetValue<string>());
    }

    [Theory]
    [InlineData("f:abc")]
    [InlineData("abc")]
    [InlineData("12a")]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("0")]
    [InlineData("00")]
    [InlineData("000")]
    [InlineData(" ")]
    [InlineData("")]
    [InlineData(null)]
    public async Task ReplyAsync_NonNumericId_SendsWithoutQuote(string? id)
    {
        (Context<TeamsActivity> context, RecordingHandler handler) = CreateContext(
            MessageActivity.FromActivity(Inbound(TeamsActivityTypes.Message, id)));

        await context.ReplyAsync("hello");

        Assert.False(HasQuote(Assert.Single(handler.Requests)));
    }

    [Fact]
    public async Task ReplyAsync_GuardedActivity_MatchesSend()
    {
        (Context<TeamsActivity> replyContext, RecordingHandler replyHandler) = CreateContext(
            MessageActivity.FromActivity(Inbound(TeamsActivityTypes.Message, "f:5d1a8c9e-3c77-4b1f-9c52-0a8f5f7a2d11")));
        (Context<TeamsActivity> sendContext, RecordingHandler sendHandler) = CreateContext(
            MessageActivity.FromActivity(Inbound(TeamsActivityTypes.Message, "f:5d1a8c9e-3c77-4b1f-9c52-0a8f5f7a2d11")));

        await replyContext.ReplyAsync("hello");
        await sendContext.SendAsync("hello");

        Sent reply = Assert.Single(replyHandler.Requests);
        Sent send = Assert.Single(sendHandler.Requests);
        Assert.Equal(send.Method, reply.Method);
        Assert.Equal(send.Path, reply.Path);
        Assert.True(JsonNode.DeepEquals(send.Body, reply.Body), $"reply: {reply.Body}\nsend: {send.Body}");
    }

    [Fact]
    public async Task ReplyAsync_InvokeWithDecimalId_Quotes()
    {
        (Context<TeamsActivity> context, RecordingHandler handler) = CreateContext(
            InvokeActivity.FromActivity(Inbound(TeamsActivityTypes.Invoke, "1759000000000")));

        await context.ReplyAsync("hello");

        Sent sent = Assert.Single(handler.Requests);
        Assert.True(HasQuote(sent));
        Assert.Contains("1759000000000", sent.Body.ToJsonString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReplyAsync_MessageWithDecimalId_Quotes()
    {
        (Context<MessageActivity> context, RecordingHandler handler) = CreateTypedContext(
            MessageActivity.FromActivity(Inbound(TeamsActivityTypes.Message, "1759000000000")));

        await context.ReplyAsync("hello");

        Assert.True(HasQuote(Assert.Single(handler.Requests)));
    }

    [Fact]
    public async Task ReplyAsync_MessageUpdateWithDecimalId_Quotes()
    {
        (Context<MessageUpdateActivity> context, RecordingHandler handler) = CreateTypedContext(
            MessageUpdateActivity.FromActivity(Inbound(TeamsActivityTypes.MessageUpdate, "1759000000000")));

        await context.ReplyAsync("hello");

        Assert.True(HasQuote(Assert.Single(handler.Requests)));
    }

    [Fact]
    public async Task ReplyAsync_MessageDeleteWithDecimalId_Quotes()
    {
        (Context<MessageDeleteActivity> context, RecordingHandler handler) = CreateTypedContext(
            MessageDeleteActivity.FromActivity(Inbound(TeamsActivityTypes.MessageDelete, "1759000000000")));

        await context.ReplyAsync("hello");

        Assert.True(HasQuote(Assert.Single(handler.Requests)));
    }

    [Fact]
    public async Task ReplyAsync_MessageReactionWithDecimalId_Quotes()
    {
        (Context<MessageReactionActivity> context, RecordingHandler handler) = CreateTypedContext(
            MessageReactionActivity.FromActivity(Inbound(TeamsActivityTypes.MessageReaction, "1759000000000")));

        await context.ReplyAsync("hello");

        Assert.True(HasQuote(Assert.Single(handler.Requests)));
    }

#pragma warning disable CS0618 // exercising the obsolete Reply(MessageActivity) overload
    [Fact]
    public async Task ObsoleteReply_SyntheticGuidId_StillSetsReplyToId()
    {
        const string syntheticId = "f:5d1a8c9e-3c77-4b1f-9c52-0a8f5f7a2d11";
        (Context<TeamsActivity> context, RecordingHandler handler) = CreateContext(
            MessageActivity.FromActivity(Inbound(TeamsActivityTypes.Message, syntheticId)));

        await context.Reply(new MessageActivity("hello"));

        Sent sent = Assert.Single(handler.Requests);
        Assert.Equal(syntheticId, sent.Body["replyToId"]?.GetValue<string>());
    }

    [Fact]
    public async Task ObsoleteReply_DecimalId_SetsReplyToId()
    {
        (Context<TeamsActivity> context, RecordingHandler handler) = CreateContext(
            MessageActivity.FromActivity(Inbound(TeamsActivityTypes.Message, "1759000000000")));

        await context.Reply(new MessageActivity("hello"));

        Sent sent = Assert.Single(handler.Requests);
        Assert.Equal("1759000000000", sent.Body["replyToId"]?.GetValue<string>());
    }
#pragma warning restore CS0618
}
