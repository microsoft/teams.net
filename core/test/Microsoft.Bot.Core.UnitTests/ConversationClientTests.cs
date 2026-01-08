// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Microsoft.Bot.Core.Schema;
using Moq;
using Moq.Protected;

namespace Microsoft.Bot.Core.UnitTests;

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

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Conversation = new Conversation { Id = "conv123" },
            ServiceUrl = new Uri("https://test.service.url/")
        };

        var result = await conversationClient.SendActivityAsync(activity);

        Assert.NotNull(result);
        Assert.Contains("activity123", result.Id);
    }

    [Fact]
    public async Task SendActivityAsync_WithNullActivity_ThrowsArgumentNullException()
    {
        HttpClient httpClient = new();
        ConversationClient conversationClient = new(httpClient);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            conversationClient.SendActivityAsync(null!));
    }

    [Fact]
    public async Task SendActivityAsync_WithNullConversation_ThrowsArgumentNullException()
    {
        HttpClient httpClient = new();
        ConversationClient conversationClient = new(httpClient);

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            ServiceUrl = new Uri("https://test.service.url/")
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            conversationClient.SendActivityAsync(activity));
    }

    [Fact]
    public async Task SendActivityAsync_WithNullConversationId_ThrowsArgumentNullException()
    {
        HttpClient httpClient = new();
        ConversationClient conversationClient = new(httpClient);

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Conversation = new Conversation() { Id = null! },
            ServiceUrl = new Uri("https://test.service.url/")
        }; ;

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            conversationClient.SendActivityAsync(activity));
    }

    [Fact]
    public async Task SendActivityAsync_WithNullServiceUrl_ThrowsArgumentNullException()
    {
        HttpClient httpClient = new();
        ConversationClient conversationClient = new(httpClient);

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Conversation = new Conversation { Id = "conv123" }
        };

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            conversationClient.SendActivityAsync(activity));
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

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Conversation = new Conversation { Id = "conv123" },
            ServiceUrl = new Uri("https://test.service.url/")
        };

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            conversationClient.SendActivityAsync(activity));

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

        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Conversation = new Conversation { Id = "conv123" },
            ServiceUrl = new Uri("https://test.service.url/")
        };

        await conversationClient.SendActivityAsync(activity);

        Assert.NotNull(capturedRequest);
        Assert.Equal("https://test.service.url/v3/conversations/conv123/activities/", capturedRequest.RequestUri?.ToString());
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
    }
}
