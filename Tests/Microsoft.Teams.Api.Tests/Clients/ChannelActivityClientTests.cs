using System.Net;
using System.Text.Json;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Common.Http;
using Moq;

namespace Microsoft.Teams.Api.Tests.Clients;

public class ChannelActivityClientTests
{
    [Fact]
    public async Task ChannelActivityClient_CreateAsync()
    {
        Resource responseResource = new Resource() { Id = "activityId" };

        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<string>()
            {
                Body = JsonSerializer.Serialize(responseResource, new JsonSerializerOptions { WriteIndented = true }),
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK
            });

        string serviceUrl = "https://serviceurl.com/";
        var channelActivityClient = new ChannelActivityClient(serviceUrl, mockHandler.Object);
        string channelId = "channelId";
        var activity = new Mock<Api.Activities.IActivity>().Object;
        var response = await channelActivityClient.CreateAsync(channelId, activity);

        Assert.Equal(responseResource.Id, response!.Id);
        string expectedUrl = "https://serviceurl.com/v3/conversations/channelId/activities";
        HttpMethod expectedMethod = HttpMethod.Post;
        mockHandler.Verify(x => x.SendAsync(
            It.Is<IHttpRequest>(arg => arg.Url == expectedUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChannelActivityClient_UpdateAsync()
    {
        Resource responseResource = new Resource() { Id = "activityId" };

        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<string>()
            {
                Body = JsonSerializer.Serialize(responseResource, new JsonSerializerOptions { WriteIndented = true }),
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK
            });

        string serviceUrl = "https://serviceurl.com/";
        var channelActivityClient = new ChannelActivityClient(serviceUrl, mockHandler.Object);
        string channelId = "channelId";
        string activityId = responseResource.Id;
        var activity = new Mock<Microsoft.Teams.Api.Activities.IActivity>().Object;
        var response = await channelActivityClient.UpdateAsync(channelId, activityId, activity);

        Assert.Equal(responseResource.Id, response!.Id);
        string expectedUrl = $"https://serviceurl.com/v3/conversations/channelId/activities/{activityId}";
        HttpMethod expectedMethod = HttpMethod.Put;
        mockHandler.Verify(x => x.SendAsync(
            It.Is<IHttpRequest>(arg => arg.Url == expectedUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChannelActivityClient_ReplyAsync()
    {
        Resource responseResource = new Resource() { Id = "activityId" };

        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<string>()
            {
                Body = JsonSerializer.Serialize(responseResource, new JsonSerializerOptions { WriteIndented = true }),
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK
            });

        string serviceUrl = "https://serviceurl.com/";
        var channelActivityClient = new ChannelActivityClient(serviceUrl, mockHandler.Object);
        string channelId = "channelId";
        string activityId = responseResource.Id;
        var activity = new Mock<Microsoft.Teams.Api.Activities.IActivity>().Object;
        var response = await channelActivityClient.ReplyAsync(channelId, activityId, activity);

        Assert.Equal(responseResource.Id, response!.Id);
        string expectedUrl = $"https://serviceurl.com/v3/conversations/channelId/activities/{activityId}";
        HttpMethod expectedMethod = HttpMethod.Post;
        mockHandler.Verify(x => x.SendAsync(
            It.Is<IHttpRequest>(arg => arg.Url == expectedUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChannelActivityClient_DeleteAsync()
    {
        Resource responseResource = new Resource() { Id = "activityId" };

        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<string>()
            {
                Body = string.Empty,
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK
            });

        string serviceUrl = "https://serviceurl.com/";
        var channelActivityClient = new ChannelActivityClient(serviceUrl, mockHandler.Object);
        string channelId = "channelId";
        string activityId = responseResource.Id;
        await channelActivityClient.DeleteAsync(channelId, activityId);

        string expectedUrl = $"https://serviceurl.com/v3/conversations/channelId/activities/{activityId}";
        HttpMethod expectedMethod = HttpMethod.Delete;
        mockHandler.Verify(x => x.SendAsync(
            It.Is<IHttpRequest>(arg => arg.Url == expectedUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
