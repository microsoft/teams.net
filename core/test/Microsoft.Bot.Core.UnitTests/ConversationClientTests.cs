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
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
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

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var conversationClient = new ConversationClient(httpClient);

        var activity = new CoreActivity
        {
            Type = ActivityTypes.Message,
            Text = "Test message",
            Conversation = new Conversation { Id = "conv123" },
            ServiceUrl = new Uri("https://test.service.url/")
        };

        // Act
        var result = await conversationClient.SendActivityAsync(activity);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("activity123", result);
    }

    [Fact]
    public async Task SendActivityAsync_WithNullActivity_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var conversationClient = new ConversationClient(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            conversationClient.SendActivityAsync(null!));
    }

    [Fact]
    public async Task SendActivityAsync_WithNullConversation_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var conversationClient = new ConversationClient(httpClient);

        var activity = new CoreActivity
        {
            Type = ActivityTypes.Message,
            Text = "Test message",
            ServiceUrl = new Uri("https://test.service.url/")
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            conversationClient.SendActivityAsync(activity));
    }

    [Fact]
    public async Task SendActivityAsync_WithNullConversationId_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var conversationClient = new ConversationClient(httpClient);

        var activity = new CoreActivity
        {
            Type = ActivityTypes.Message,
            Text = "Test message",
            Conversation = new Conversation(),
            ServiceUrl = new Uri("https://test.service.url/")
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            conversationClient.SendActivityAsync(activity));
    }

    [Fact]
    public async Task SendActivityAsync_WithNullServiceUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient();
        var conversationClient = new ConversationClient(httpClient);

        var activity = new CoreActivity
        {
            Type = ActivityTypes.Message,
            Text = "Test message",
            Conversation = new Conversation { Id = "conv123" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            conversationClient.SendActivityAsync(activity));
    }

    [Fact]
    public async Task SendActivityAsync_WithHttpError_ThrowsHttpRequestException()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
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

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var conversationClient = new ConversationClient(httpClient);

        var activity = new CoreActivity
        {
            Type = ActivityTypes.Message,
            Text = "Test message",
            Conversation = new Conversation { Id = "conv123" },
            ServiceUrl = new Uri("https://test.service.url/")
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            conversationClient.SendActivityAsync(activity));

        Assert.Contains("Error sending activity", exception.Message);
        Assert.Contains("BadRequest", exception.Message);
    }

    [Fact]
    public async Task SendActivityAsync_ConstructsCorrectUrl()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
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

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var conversationClient = new ConversationClient(httpClient);

        var activity = new CoreActivity
        {
            Type = ActivityTypes.Message,
            Text = "Test message",
            Conversation = new Conversation { Id = "conv123" },
            ServiceUrl = new Uri("https://test.service.url/")
        };

        // Act
        await conversationClient.SendActivityAsync(activity);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("https://test.service.url/v3/conversations/conv123/activities/", capturedRequest.RequestUri?.ToString());
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
    }
}
