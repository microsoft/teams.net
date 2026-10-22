// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;
using Moq;
using Moq.Protected;

namespace Microsoft.Teams.Apps.UnitTests;

public class ConversationApiClientTests
{
    private static readonly Uri ServiceUrl = new("https://test.service.url/");

    [Fact]
    public async Task ReplyChainMethods_NullActivity_ThrowArgumentNullExceptionBeforeAnyRequest()
    {
        (Mock<ConversationClient> conversationClient, ReplyCapture capture, _) = CreateConversationClient();
        ConversationApiClient client = new(ServiceUrl, conversationClient.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateReplyChainActivityAsync("chat-id", "root-id", null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateTargetedReplyChainActivityAsync("chat-id", "root-id", null!));

        Assert.Equal(0, capture.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReplyChainMethods_CallReplyChainsWithIdsAndTargetedFlag(bool isTargeted)
    {
        (Mock<ConversationClient> conversationClient, ReplyCapture capture, _) = CreateConversationClient();
        ConversationApiClient client = new(ServiceUrl, conversationClient.Object);
        MessageActivityInput activity = new MessageActivityInput().WithText("hello");

        if (isTargeted)
        {
            await client.CreateTargetedReplyChainActivityAsync("chat-id", "root-id", activity);
        }
        else
        {
            await client.CreateReplyChainActivityAsync("chat-id", "root-id", activity);
        }

        Assert.Equal("chat-id", capture.ConversationId);
        Assert.Equal("root-id", capture.ActivityId);
        Assert.Same(activity, capture.Activity);
        Assert.Equal(ServiceUrl, capture.ServiceUrl);
        Assert.Equal(isTargeted, capture.IsTargeted);
        Assert.Equal(1, capture.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReplyChainMethods_DoNotSetReplyToIdAndDoNotCreateAnActivity(bool isTargeted)
    {
        (Mock<ConversationClient> conversationClient, ReplyCapture capture, _) = CreateConversationClient();
        ConversationApiClient client = new(ServiceUrl, conversationClient.Object);
        MessageActivityInput activity = new MessageActivityInput().WithText("hello");

        if (isTargeted)
        {
            await client.CreateTargetedReplyChainActivityAsync("chat-id", "root-id", activity);
        }
        else
        {
            await client.CreateReplyChainActivityAsync("chat-id", "root-id", activity);
        }

        Assert.Null(activity.ReplyToId);
        Assert.Null(capture.Activity?.ReplyToId);
        VerifyNoCreate(conversationClient);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReplyChainMethods_PassAgenticIdentityHeadersAndCancellationToken(bool isTargeted)
    {
        (Mock<ConversationClient> conversationClient, ReplyCapture capture, _) = CreateConversationClient();
        AgenticIdentity identity = new() { AgenticAppId = "app-123", AgenticUserId = "user-456" };
        ConversationApiClient client = new(ServiceUrl, conversationClient.Object, identity);
        Dictionary<string, string> headers = new() { ["X-Test-Header"] = "header-value" };
        using CancellationTokenSource cts = new();
        MessageActivityInput activity = new MessageActivityInput().WithText("hello");

        if (isTargeted)
        {
            await client.CreateTargetedReplyChainActivityAsync("chat-id", "root-id", activity, headers, cts.Token);
        }
        else
        {
            await client.CreateReplyChainActivityAsync("chat-id", "root-id", activity, headers, cts.Token);
        }

        Assert.Equal(1, capture.Calls);
        Assert.Same(identity, capture.RequestContext?.AgenticIdentity);
        Assert.Same(headers, capture.Headers);
        Assert.Equal(cts.Token, capture.CancellationToken);
    }

    [Fact]
    public async Task ReplyChainMethods_BlankIds_ThrowBeforeAnyRequest()
    {
        (ConversationApiClient client, WireCapture wire) = CreateWireClient();
        MessageActivityInput activity = new MessageActivityInput().WithText("hello");

        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.CreateReplyChainActivityAsync(" ", "root-id", activity));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.CreateReplyChainActivityAsync("chat-id", "", activity));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.CreateTargetedReplyChainActivityAsync(" ", "root-id", activity));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.CreateTargetedReplyChainActivityAsync("chat-id", "", activity));

        Assert.Equal(0, wire.Requests);
    }

    [Theory]
    [InlineData(false, "https://test.service.url/v3/conversations/19%3Achat%40unq.gbl.spaces/replyChains/root%2F1")]
    [InlineData(true, "https://test.service.url/v3/conversations/19%3Achat%40unq.gbl.spaces/replyChains/root%2F1?isTargetedActivity=true")]
    public async Task ReplyChainMethods_PostToTheReplyChainsRouteWithEscapedIdsAndNoReplyToId(bool isTargeted, string expectedUrl)
    {
        (ConversationApiClient client, WireCapture wire) = CreateWireClient();
        MessageActivityInput activity = new MessageActivityInput().WithText("hello");

        if (isTargeted)
        {
            await client.CreateTargetedReplyChainActivityAsync("19:chat@unq.gbl.spaces", "root/1", activity);
        }
        else
        {
            await client.CreateReplyChainActivityAsync("19:chat@unq.gbl.spaces", "root/1", activity);
        }

        Assert.Equal(HttpMethod.Post, wire.Method);
        Assert.Equal(expectedUrl, wire.Url);
        Assert.Contains("hello", wire.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("replyToId", wire.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReplyToActivityAsync_PostsToTheActivitiesRoute()
    {
        (ConversationApiClient client, WireCapture wire) = CreateWireClient();
        MessageActivityInput activity = new MessageActivityInput().WithText("hello");

        await client.ReplyToActivityAsync("19:abc@thread.tacv2", "msg-1", activity);

        Assert.Equal(HttpMethod.Post, wire.Method);
        Assert.Equal("https://test.service.url/v3/conversations/19%3Aabc%40thread.tacv2/activities/", wire.Url);
        Assert.DoesNotContain("replyChains", wire.Url, StringComparison.Ordinal);
        Assert.Contains("hello", wire.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReplyToActivityAsync_StampsReplyToId()
    {
        (ConversationApiClient client, WireCapture wire) = CreateWireClient();
        MessageActivityInput activity = new MessageActivityInput().WithText("hello");

        await client.ReplyToActivityAsync("conv-1", "msg-1", activity);

        Assert.Equal("msg-1", activity.ReplyToId);
        using JsonDocument json = JsonDocument.Parse(wire.Body);
        Assert.Equal("msg-1", json.RootElement.GetProperty("replyToId").GetString());
    }

    [Fact]
    public async Task ReplyToActivityAsync_SendsThroughSendActivityToTheGivenConversation()
    {
        (Mock<ConversationClient> conversationClient, ReplyCapture reply, SendCapture send) = CreateConversationClient();
        ConversationApiClient client = new(ServiceUrl, conversationClient.Object);
        MessageActivityInput activity = new MessageActivityInput().WithText("hello");

        await client.ReplyToActivityAsync("conv-1", "msg-1", activity);

        Assert.Equal("conv-1", send.ConversationId);
        Assert.Same(activity, send.Activity);
        Assert.False(send.IsTargeted);
        Assert.Equal(0, reply.Calls);
    }

    [Fact]
    public async Task ReplyToActivityAsync_PassesAgenticIdentityHeadersAndCancellationToken()
    {
        (Mock<ConversationClient> conversationClient, _, SendCapture send) = CreateConversationClient();
        AgenticIdentity identity = new() { AgenticAppId = "app-123", AgenticUserId = "user-456" };
        ConversationApiClient client = new(ServiceUrl, conversationClient.Object, identity);
        Dictionary<string, string> headers = new() { ["X-Test-Header"] = "header-value" };
        using CancellationTokenSource cts = new();

        await client.ReplyToActivityAsync("conv-1", "msg-1", new MessageActivityInput().WithText("hello"), headers, cts.Token);

        Assert.Same(identity, send.RequestContext?.AgenticIdentity);
        Assert.Same(headers, send.Headers);
        Assert.Equal(cts.Token, send.CancellationToken);
    }

    [Fact]
    public async Task ReplyToActivityAsync_NullActivity_ThrowsArgumentNullExceptionBeforeAnyRequest()
    {
        (ConversationApiClient client, WireCapture wire) = CreateWireClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.ReplyToActivityAsync("conv-1", "msg-1", null!));

        Assert.Equal(0, wire.Requests);
    }

    [Fact]
    public async Task ObsoleteActivityClient_ReplyAsync_StampsReplyToIdAndSendsToTheConversation()
    {
        (Mock<ConversationClient> conversationClient, ReplyCapture reply, SendCapture send) = CreateConversationClient();
        AgenticIdentity identity = new() { AgenticAppId = "app-123", AgenticUserId = "user-456" };
        Dictionary<string, string> headers = new() { ["X-Test-Header"] = "header-value" };
        MessageActivityInput activity = new MessageActivityInput().WithText("hello");

#pragma warning disable CS0618 // exercising the obsolete ActivityClient
        ActivityClient client = new(ServiceUrl, conversationClient.Object, identity);
        await client.ReplyAsync("conv-1", "msg-1", activity, headers);
#pragma warning restore CS0618

        Assert.Equal("msg-1", activity.ReplyToId);
        Assert.Equal("conv-1", send.ConversationId);
        Assert.Equal("msg-1", send.Activity?.ReplyToId);
        Assert.False(send.IsTargeted);
        Assert.Same(identity, send.RequestContext?.AgenticIdentity);
        Assert.Same(headers, send.Headers);
        Assert.Equal(0, reply.Calls);
    }

    [Fact]
    public async Task ObsoleteActivityClient_ReplyAsync_InboundActivityOverload_StampsReplyToIdAndSendsToTheConversation()
    {
        (Mock<ConversationClient> conversationClient, ReplyCapture reply, SendCapture send) = CreateConversationClient();

#pragma warning disable CS0618 // exercising the obsolete ActivityClient and inbound-activity overload
        ActivityClient client = new(ServiceUrl, conversationClient.Object);
        await client.ReplyAsync("conv-1", "msg-1", new MessageActivity("hello"));
#pragma warning restore CS0618

        Assert.Equal("conv-1", send.ConversationId);
        Assert.Equal("msg-1", send.Activity?.ReplyToId);
        Assert.False(send.IsTargeted);
        Assert.Equal(0, reply.Calls);
    }

    private sealed class ReplyCapture
    {
        public int Calls { get; set; }
        public string? ConversationId { get; set; }
        public string? ActivityId { get; set; }
        public CoreActivityInput? Activity { get; set; }
        public Uri? ServiceUrl { get; set; }
        public bool IsTargeted { get; set; }
        public BotRequestContext? RequestContext { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }

    private sealed class SendCapture
    {
        public string? ConversationId { get; set; }
        public CoreActivityInput? Activity { get; set; }
        public bool IsTargeted { get; set; }
        public BotRequestContext? RequestContext { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }

    private sealed class WireCapture
    {
        public int Requests { get; set; }
        public HttpMethod? Method { get; set; }
        public string? Url { get; set; }
        public string Body { get; set; } = string.Empty;
    }

    private static (Mock<ConversationClient> ConversationClient, ReplyCapture Reply, SendCapture Send) CreateConversationClient()
    {
        Mock<ConversationClient> conversationClient = new(new HttpClient(), NullLogger<ConversationClient>.Instance);
        ReplyCapture reply = new();
        SendCapture send = new();
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
                (conversationId, activityId, activity, serviceUrl, isTargeted, requestContext, headers, cancellationToken) =>
                {
                    reply.Calls++;
                    reply.ConversationId = conversationId;
                    reply.ActivityId = activityId;
                    reply.Activity = activity;
                    reply.ServiceUrl = serviceUrl;
                    reply.IsTargeted = isTargeted;
                    reply.RequestContext = requestContext;
                    reply.Headers = headers;
                    reply.CancellationToken = cancellationToken;
                })
            .ReturnsAsync(new SendActivityResponse { Id = "reply-id" });
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
                (conversationId, activity, _, isTargeted, requestContext, headers, cancellationToken) =>
                {
                    send.ConversationId = conversationId;
                    send.Activity = activity;
                    send.IsTargeted = isTargeted;
                    send.RequestContext = requestContext;
                    send.Headers = headers;
                    send.CancellationToken = cancellationToken;
                })
            .ReturnsAsync(new SendActivityResponse { Id = "sent-id" });
        return (conversationClient, reply, send);
    }

    private static (ConversationApiClient Client, WireCapture Wire) CreateWireClient()
    {
        WireCapture wire = new();
        Mock<HttpMessageHandler> handler = new();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken _) =>
            {
                wire.Requests++;
                wire.Method = request.Method;
                wire.Url = request.RequestUri?.OriginalString;
                wire.Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"id\":\"reply-id\"}") };
            });
        return (new ConversationApiClient(ServiceUrl, new ConversationClient(new HttpClient(handler.Object))), wire);
    }

    private static void VerifyNoCreate(Mock<ConversationClient> conversationClient)
    {
        conversationClient.Verify(
            c => c.SendActivityAsync(
                It.IsAny<string>(),
                It.IsAny<CoreActivityInput>(),
                It.IsAny<Uri>(),
                It.IsAny<bool>(),
                It.IsAny<BotRequestContext?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
