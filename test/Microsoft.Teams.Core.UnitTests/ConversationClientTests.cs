// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;
using Moq;
using Moq.Protected;

namespace Microsoft.Teams.Core.UnitTests;

public class ConversationClientTests
{
    [Fact]
    public async Task SendActivityAsync_WithValidActivity_SendsSuccessfully()
    {
        Mock<HttpMessageHandler> mockHttpMessageHandler = new();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"id\":\"activity123\"}")
            });

        HttpClient httpClient = new(mockHttpMessageHandler.Object);
        ConversationClient conversationClient = new(httpClient);

        SendActivityResponse? result = await conversationClient.SendActivityAsync("conv123", CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(), new Uri("https://test.service.url/"));

        Assert.NotNull(result);
        Assert.Contains("activity123", result.Id);
    }

    [Fact]
    public async Task SendActivityAsync_WithNullActivity_ThrowsArgumentNullException()
    {
        HttpClient httpClient = new();
        ConversationClient conversationClient = new(httpClient);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            conversationClient.SendActivityAsync("conv123", null!, new Uri("https://test.service.url/")));
    }

    [Fact]
    public async Task SendActivityAsync_WithNullConversationId_ThrowsArgumentNullException()
    {
        HttpClient httpClient = new();
        ConversationClient conversationClient = new(httpClient);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            conversationClient.SendActivityAsync(null!, CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(), new Uri("https://test.service.url/")));
    }

    [Fact]
    public async Task SendActivityAsync_WithEmptyConversationId_ThrowsArgumentException()
    {
        HttpClient httpClient = new();
        ConversationClient conversationClient = new(httpClient);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            conversationClient.SendActivityAsync("", CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(), new Uri("https://test.service.url/")));
    }

    [Fact]
    public async Task SendActivityAsync_WithNullServiceUrl_ThrowsArgumentNullException()
    {
        HttpClient httpClient = new();
        ConversationClient conversationClient = new(httpClient);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            conversationClient.SendActivityAsync("conv123", CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(), null!));
    }

    [Fact]
    public async Task SendActivityAsync_WithHttpError_ThrowsHttpRequestException()
    {
        Mock<HttpMessageHandler> mockHttpMessageHandler = new();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Bad request error")
            });

        HttpClient httpClient = new(mockHttpMessageHandler.Object);
        ConversationClient conversationClient = new(httpClient);

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            conversationClient.SendActivityAsync("conv123", CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(), new Uri("https://test.service.url/")));

        Assert.Contains("Error sending activity", exception.Message);
        Assert.Contains("BadRequest", exception.Message);
    }

    [Fact]
    public async Task SendActivityAsync_ConstructsCorrectUrl()
    {
        HttpRequestMessage? capturedRequest = null;
        Mock<HttpMessageHandler> mockHttpMessageHandler = new();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"id\":\"activity123\"}")
            });

        HttpClient httpClient = new(mockHttpMessageHandler.Object);
        ConversationClient conversationClient = new(httpClient);

        await conversationClient.SendActivityAsync("conv123", CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(), new Uri("https://test.service.url/"));

        Assert.NotNull(capturedRequest);
        Assert.Equal("https://test.service.url/v3/conversations/conv123/activities/", capturedRequest.RequestUri?.ToString());
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
    }

    [Fact]
    public async Task SendActivityAsync_WithIsTargeted_AppendsQueryString()
    {
        HttpRequestMessage? capturedRequest = null;
        Mock<HttpMessageHandler> mockHttpMessageHandler = new();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"id\":\"activity123\"}")
            });

        HttpClient httpClient = new(mockHttpMessageHandler.Object);
        ConversationClient conversationClient = new(httpClient);

        await conversationClient.SendActivityAsync("conv123", CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(), new Uri("https://test.service.url/"), isTargeted: true);

        Assert.NotNull(capturedRequest);
        Assert.Contains("isTargetedActivity=true", capturedRequest.RequestUri?.ToString());
    }

    [Fact]
    public async Task UpdateActivityAsync_WithIsTargeted_AppendsQueryString()
    {
        HttpRequestMessage? capturedRequest = null;
        Mock<HttpMessageHandler> mockHttpMessageHandler = new();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"id\":\"activity123\"}")
            });

        HttpClient httpClient = new(mockHttpMessageHandler.Object);
        ConversationClient conversationClient = new(httpClient, NullLogger<ConversationClient>.Instance);

        await conversationClient.UpdateActivityAsync("conv123", "activity123", CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(), new Uri("https://test.service.url/"), isTargeted: true);

        Assert.NotNull(capturedRequest);
        Assert.Contains("isTargetedActivity=true", capturedRequest.RequestUri?.ToString());
        Assert.Equal(HttpMethod.Put, capturedRequest.Method);
    }

    [Fact]
    public async Task UpdateActivityAsync_WithIsTargeted_DoesNotSendRecipient()
    {
        string? capturedBody = null;
        Mock<HttpMessageHandler> mockHttpMessageHandler = new();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage req, CancellationToken ct) =>
            {
                capturedBody = req.Content is null ? null : await req.Content.ReadAsStringAsync(ct);
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"id\":\"activity123\"}")
                };
            });

        HttpClient httpClient = new(mockHttpMessageHandler.Object);
        ConversationClient conversationClient = new(httpClient, NullLogger<ConversationClient>.Instance);

        await conversationClient.UpdateActivityAsync(
            "conv123",
            "activity123",
            CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(),
            new Uri("https://test.service.url/"),
            isTargeted: true);

        Assert.NotNull(capturedBody);
        using JsonDocument document = JsonDocument.Parse(capturedBody);
        Assert.False(
            document.RootElement.TryGetProperty("recipient", out _),
            "Targeted update payloads must omit 'recipient'; the service rejects it on update.");
    }

    [Fact]
    public async Task DeleteActivityAsync_WithIsTargeted_AppendsQueryString()
    {
        HttpRequestMessage? capturedRequest = null;
        Mock<HttpMessageHandler> mockHttpMessageHandler = new();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        HttpClient httpClient = new(mockHttpMessageHandler.Object);
        ConversationClient conversationClient = new(httpClient, NullLogger<ConversationClient>.Instance);

        await conversationClient.DeleteActivityAsync(
            "conv123",
            "activity123",
            new Uri("https://test.service.url/"),
            isTargeted: true);

        Assert.NotNull(capturedRequest);
        Assert.Contains("isTargetedActivity=true", capturedRequest.RequestUri?.ToString());
        Assert.Equal(HttpMethod.Delete, capturedRequest.Method);
    }

    [Fact]
    public async Task DeleteActivityAsync_WithActivity_UsesIsTargetedProperty()
    {
        HttpRequestMessage? capturedRequest = null;
        Mock<HttpMessageHandler> mockHttpMessageHandler = new();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        HttpClient httpClient = new(mockHttpMessageHandler.Object);
        ConversationClient conversationClient = new(httpClient, NullLogger<ConversationClient>.Instance);

        CoreActivity activity = new()
        {
            Id = "activity123",
            Type = ActivityType.Message,
            ServiceUrl = new Uri("https://test.service.url/")
        };

        await conversationClient.DeleteActivityAsync("conv123", activity.Id!, activity.ServiceUrl!, isTargeted: true);

        Assert.NotNull(capturedRequest);
        Assert.Contains("isTargetedActivity=true", capturedRequest.RequestUri?.ToString());
        Assert.Equal(HttpMethod.Delete, capturedRequest.Method);
    }

    [Fact]
    public async Task SendActivityAsync_WithChannelAccountFrom_ExtractsAgenticIdentity()
    {
        Mock<HttpMessageHandler> mockHttpMessageHandler = new();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"id\":\"activity123\"}")
            });

        HttpClient httpClient = new(mockHttpMessageHandler.Object);
        ConversationClient conversationClient = new(httpClient);

        ChannelAccount from = new() { Id = "bot1", AgenticAppId = "app-123", AgenticUserId = "user-456" };

        SendActivityResponse? result = await conversationClient.SendActivityAsync("conv123", CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).WithProperty("from", from).Build(), new Uri("https://test.service.url/"));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task SendReplyChainActivityAsync_PostsToReplyChainsRoute()
    {
        RequestCapture capture = new();
        ConversationClient conversationClient = new(new HttpClient(capture.CreateHandler()));

        SendActivityResponse? result = await conversationClient.SendReplyChainActivityAsync(
            "conv123",
            "root456",
            CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(),
            new Uri("https://test.service.url/"));

        Assert.Equal("reply123", result?.Id);
        Assert.Equal("https://test.service.url/v3/conversations/conv123/replyChains/root456", capture.Request?.RequestUri?.OriginalString);
        Assert.Equal(HttpMethod.Post, capture.Request?.Method);
    }

    [Fact]
    public async Task SendReplyChainActivityAsync_EscapesBothIds()
    {
        RequestCapture capture = new();
        ConversationClient conversationClient = new(new HttpClient(capture.CreateHandler()));

        await conversationClient.SendReplyChainActivityAsync(
            "19:abc/def@thread.v2",
            "root/456 789",
            CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(),
            new Uri("https://test.service.url/"));

        Assert.Equal(
            "https://test.service.url/v3/conversations/19%3Aabc%2Fdef%40thread.v2/replyChains/root%2F456%20789",
            capture.Request?.RequestUri?.OriginalString);
    }

    [Fact]
    public async Task SendReplyChainActivityAsync_DoesNotAppendAMessageIdSuffix()
    {
        RequestCapture capture = new();
        ConversationClient conversationClient = new(new HttpClient(capture.CreateHandler()));

        await conversationClient.SendReplyChainActivityAsync(
            "19:abc@thread.v2",
            "root456",
            CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(),
            new Uri("https://test.service.url/"));

        Assert.Equal("https://test.service.url/v3/conversations/19%3Aabc%40thread.v2/replyChains/root456", capture.Request?.RequestUri?.OriginalString);
    }

    [Fact]
    public async Task SendReplyChainActivityAsync_DoesNotSetReplyToId()
    {
        RequestCapture capture = new();
        ConversationClient conversationClient = new(new HttpClient(capture.CreateHandler()));

        await conversationClient.SendReplyChainActivityAsync(
            "conv123",
            "root456",
            CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).WithProperty("text", "hello").Build(),
            new Uri("https://test.service.url/"));

        Assert.NotNull(capture.Body);
        Assert.Contains("hello", capture.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("replyToId", capture.Body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false, "https://test.service.url/v3/conversations/conv123/replyChains/root456")]
    [InlineData(true, "https://test.service.url/v3/conversations/conv123/replyChains/root456?isTargetedActivity=true")]
    public async Task SendReplyChainActivityAsync_AppendsTargetedQueryOnlyWhenTargeted(bool isTargeted, string expectedUrl)
    {
        RequestCapture capture = new();
        ConversationClient conversationClient = new(new HttpClient(capture.CreateHandler()));

        await conversationClient.SendReplyChainActivityAsync(
            "conv123",
            "root456",
            CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(),
            new Uri("https://test.service.url/"),
            isTargeted: isTargeted);

        Assert.Equal(expectedUrl, capture.Request?.RequestUri?.OriginalString);
    }

    [Theory]
    [InlineData(null, "root456")]
    [InlineData("", "root456")]
    [InlineData("   ", "root456")]
    [InlineData("conv123", null)]
    [InlineData("conv123", "")]
    [InlineData("conv123", "   ")]
    public async Task SendReplyChainActivityAsync_BlankIds_ThrowBeforeAnyRequest(string? conversationId, string? activityId)
    {
        RequestCapture capture = new();
        ConversationClient conversationClient = new(new HttpClient(capture.CreateHandler()));

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            conversationClient.SendReplyChainActivityAsync(
                conversationId!,
                activityId!,
                CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(),
                new Uri("https://test.service.url/")));

        Assert.Null(capture.Request);
    }

    [Fact]
    public async Task SendReplyChainActivityAsync_PassesThroughCustomHeadersAndRequestContext()
    {
        RequestCapture capture = new();
        ConversationClient conversationClient = new(new HttpClient(capture.CreateHandler()));
        AgenticIdentity identity = new() { AgenticAppId = "app-123", AgenticUserId = "user-456" };

        await conversationClient.SendReplyChainActivityAsync(
            "conv123",
            "root456",
            CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(),
            new Uri("https://test.service.url/"),
            requestContext: BotRequestContext.FromAgenticIdentity(identity),
            customHeaders: new Dictionary<string, string> { ["X-Test-Header"] = "header-value" });

        Assert.NotNull(capture.Request);
        Assert.Equal("header-value", Assert.Single(capture.Request.Headers.GetValues("X-Test-Header")));
        Assert.True(capture.Request.Options.TryGetValue(new HttpRequestOptionsKey<object?>(BotRequestContext.AgenticIdentityKey), out object? stamped));
        Assert.Same(identity, stamped);
    }

    [Theory]
    [InlineData(".", "root456")]
    [InlineData("..", "root456")]
    [InlineData("conv123", ".")]
    [InlineData("conv123", "..")]
    public async Task SendReplyChainActivityAsync_DotSegmentIds_ThrowBeforeAnyRequest(string conversationId, string activityId)
    {
        RequestCapture capture = new();
        ConversationClient conversationClient = new(new HttpClient(capture.CreateHandler()));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            conversationClient.SendReplyChainActivityAsync(
                conversationId,
                activityId,
                CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(),
                new Uri("https://test.service.url/")));

        Assert.Null(capture.Request);
    }

    [Theory]
    [InlineData("...", "root456")]
    [InlineData(".a", "root456")]
    [InlineData("conv123", "..a")]
    public async Task SendReplyChainActivityAsync_IdsThatOnlyContainDots_AreSent(string conversationId, string activityId)
    {
        RequestCapture capture = new();
        ConversationClient conversationClient = new(new HttpClient(capture.CreateHandler()));

        await conversationClient.SendReplyChainActivityAsync(
            conversationId,
            activityId,
            CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(),
            new Uri("https://test.service.url/"));

        Assert.Equal($"https://test.service.url/v3/conversations/{conversationId}/replyChains/{activityId}", capture.Request?.RequestUri?.OriginalString);
    }

    [Fact]
    public async Task SendReplyChainActivityAsync_WithHttpError_ThrowsHttpRequestException()
    {
        RequestCapture capture = new() { StatusCode = HttpStatusCode.BadRequest };
        ConversationClient conversationClient = new(new HttpClient(capture.CreateHandler()));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            conversationClient.SendReplyChainActivityAsync(
                "conv123",
                "root456",
                CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(),
                new Uri("https://test.service.url/")));
    }

    [Fact]
    public async Task SendReplyChainActivityAsync_WithNullActivity_ThrowsArgumentNullException()
    {
        ConversationClient conversationClient = new(new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            conversationClient.SendReplyChainActivityAsync("conv123", "root456", null!, new Uri("https://test.service.url/")));
    }

    [Fact]
    public async Task SendReplyChainActivityAsync_WithNullServiceUrl_ThrowsArgumentNullException()
    {
        ConversationClient conversationClient = new(new HttpClient());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            conversationClient.SendReplyChainActivityAsync(
                "conv123",
                "root456",
                CoreActivityInput.CreateBuilder().WithType(ActivityType.Message).Build(),
                null!));
    }

    private sealed class RequestCapture
    {
        public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;

        public HttpRequestMessage? Request { get; private set; }

        public string? Body { get; private set; }

        public HttpMessageHandler CreateHandler()
        {
            Mock<HttpMessageHandler> handler = new();
            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, _) =>
                {
                    Request = request;
                    Body = request.Content is null ? null : await request.Content.ReadAsStringAsync();
                    return new HttpResponseMessage
                    {
                        StatusCode = StatusCode,
                        Content = new StringContent("{\"id\":\"reply123\"}")
                    };
                });
            return handler.Object;
        }
    }
}
