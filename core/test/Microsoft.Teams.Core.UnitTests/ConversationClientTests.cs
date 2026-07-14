// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
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
    public async Task SendActivityAsync_WithTargetedRecipient_AppendsQueryString()
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

        // The recipient carries IsTargeted; routing must target even though isTargeted is not passed.
        CoreActivityInput activity = CoreActivityInput.CreateBuilder()
            .WithType(ActivityType.Message)
            .WithRecipient(new ChannelAccount { Id = "user-1", IsTargeted = true })
            .Build();

        await conversationClient.SendActivityAsync("conv123", activity, new Uri("https://test.service.url/"));

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

        await conversationClient.DeleteActivityAsync("conv123", activity, isTargeted: true);

        Assert.NotNull(capturedRequest);
        Assert.Contains("isTargetedActivity=true", capturedRequest.RequestUri?.ToString());
        Assert.Equal(HttpMethod.Delete, capturedRequest.Method);
    }

    [Fact]
    public async Task SendActivityAsync_WithJsonElementFrom_ExtractsAgenticIdentity()
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

        // Simulate a deserialized activity with agentic identity properties in "from"
        string activityJson = """
        {
            "type": "message",
            "serviceUrl": "https://test.service.url/",
            "conversation": { "id": "conv123" },
            "from": { "id": "bot1", "agenticAppId": "app-123", "agenticUserId": "user-456" }
        }
        """;
        CoreActivity activity = CoreActivity.FromJsonString(activityJson);

        await conversationClient.SendActivityAsync(activity.Conversation!.Id!, CoreActivityInput.FromActivity(activity), activity.ServiceUrl!);

        // Verify the request was made (agenticIdentity is passed to BotHttpClient via request options)
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
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

}
