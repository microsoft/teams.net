using System.Net;
using System.Text.Json;

using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Common.Http;

using Moq;

using static Microsoft.Teams.Api.Activities.Invokes.Configs;

namespace Microsoft.Teams.Api.Tests.Clients;

public class ActivityClientTests
{
    [Fact]
    public async Task ActivityClient_CreateAsync()
    {
        Resource responseResource = new Resource() { Id = "activityId" };

        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var responseBody = new Resource() { Id = "activityId" };
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
        var activityClient = new ActivityClient(serviceUrl, mockHandler.Object);
        string conversationId = "conversationId";
        var value = new Cards.HeroCard()
        {
            Title = "test card",
            SubTitle = "test fetch config activity"
        };
        var activity = new FetchActivity(value);
        var response = await activityClient.CreateAsync(conversationId, activity);

        Assert.Equal(responseResource.Id, response!.Id);
        string expecteUrl = "https://serviceurl.com/v3/conversations/conversationId/activities";
        HttpMethod expectedMethod = HttpMethod.Post;
        mockHandler.Verify(x => x.SendAsync(
            It.Is<IHttpRequest>(arg => arg.Url == expecteUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [Fact]
    public async Task ActivityClient_CreateAsync_NullResponse()
    {
        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var responseBody = new Resource() { Id = "activityId" };
        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<string>()
            {
                Body = String.Empty,
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK
            });

        string serviceUrl = "https://serviceurl.com/";
        var activityClient = new ActivityClient(serviceUrl, mockHandler.Object);
        string conversationId = "conversationId";
        var value = new Cards.HeroCard()
        {
            Title = "test card",
            SubTitle = "test fetch config activity"
        };
        var activity = new FetchActivity(value);
        var response = await activityClient.CreateAsync(conversationId, activity);

        Assert.Null(response);
        string expecteUrl = "https://serviceurl.com/v3/conversations/conversationId/activities";
        HttpMethod expectedMethod = HttpMethod.Post;
        mockHandler.Verify(x => x.SendAsync(
            It.Is<IHttpRequest>(arg => arg.Url == expecteUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [Fact]
    public async Task ActivityClient_UpdateAsync()
    {
        Resource responseResource = new Resource() { Id = "activityId" };

        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var responseBody = new Resource() { Id = "activityId" };
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
        var activityClient = new ActivityClient(serviceUrl, mockHandler.Object);
        string conversationId = "conversationId";
        var value = new Cards.HeroCard()
        {
            Title = "test card",
            SubTitle = "test fetch config activity"
        };
        var activity = new FetchActivity(value);
        var response = await activityClient.UpdateAsync(conversationId, responseResource.Id, activity);

        Assert.Equal(responseResource.Id, response!.Id);
        string expecteUrl = "https://serviceurl.com/v3/conversations/conversationId/activities/activityId";
        HttpMethod expectedMethod = HttpMethod.Put;
        mockHandler.Verify(x => x.SendAsync(
            It.Is<IHttpRequest>(arg => arg.Url == expecteUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [Fact]
    public async Task ActivityClient_ReplyAsync()
    {
        Resource responseResource = new Resource() { Id = "activityId" };

        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var responseBody = new Resource() { Id = "activityId" };
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
        var activityClient = new ActivityClient(serviceUrl, mockHandler.Object);
        string conversationId = "conversationId";
        var value = new Cards.HeroCard()
        {
            Title = "test card",
            SubTitle = "test fetch config activity"
        };
        var activity = new FetchActivity(value);
        var response = await activityClient.ReplyAsync(conversationId, responseResource.Id, activity);

        Assert.Equal(responseResource.Id, response!.Id);
        string expecteUrl = "https://serviceurl.com/v3/conversations/conversationId/activities/activityId";
        HttpMethod expectedMethod = HttpMethod.Post;
        mockHandler.Verify(x => x.SendAsync(
            It.Is<IHttpRequest>(arg => arg.Url == expecteUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ActivityClient_CreateAsync_WithTargeted()
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
        var activityClient = new ActivityClient(serviceUrl, mockHandler.Object);
        string conversationId = "conversationId";
        var value = new Cards.HeroCard()
        {
            Title = "test card",
            SubTitle = "test targeted activity"
        };
        var activity = new FetchActivity(value);
        var response = await activityClient.CreateAsync(conversationId, activity, isTargeted: true);

        Assert.Equal(responseResource.Id, response!.Id);
        string expectedUrl = "https://serviceurl.com/v3/conversations/conversationId/activities?isTargetedActivity=true";
        HttpMethod expectedMethod = HttpMethod.Post;
        mockHandler.Verify(x => x.SendAsync(
            It.Is<IHttpRequest>(arg => arg.Url == expectedUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ActivityClient_UpdateAsync_WithTargeted()
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
        var activityClient = new ActivityClient(serviceUrl, mockHandler.Object);
        string conversationId = "conversationId";
        var value = new Cards.HeroCard()
        {
            Title = "test card",
            SubTitle = "test targeted update activity"
        };
        var activity = new FetchActivity(value);
        var response = await activityClient.UpdateAsync(conversationId, responseResource.Id, activity, isTargeted: true);

        Assert.Equal(responseResource.Id, response!.Id);
        string expectedUrl = "https://serviceurl.com/v3/conversations/conversationId/activities/activityId?isTargetedActivity=true";
        HttpMethod expectedMethod = HttpMethod.Put;
        mockHandler.Verify(x => x.SendAsync(
            It.Is<IHttpRequest>(arg => arg.Url == expectedUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ActivityClient_ReplyAsync_WithTargeted()
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
        var activityClient = new ActivityClient(serviceUrl, mockHandler.Object);
        string conversationId = "conversationId";
        var value = new Cards.HeroCard()
        {
            Title = "test card",
            SubTitle = "test targeted reply activity"
        };
        var activity = new FetchActivity(value);
        var response = await activityClient.ReplyAsync(conversationId, responseResource.Id, activity, isTargeted: true);

        Assert.Equal(responseResource.Id, response!.Id);
        string expectedUrl = "https://serviceurl.com/v3/conversations/conversationId/activities/activityId?isTargetedActivity=true";
        HttpMethod expectedMethod = HttpMethod.Post;
        mockHandler.Verify(x => x.SendAsync(
            It.Is<IHttpRequest>(arg => arg.Url == expectedUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ActivityClient_DeleteAsync()
    {
        Resource responseResource = new Resource() { Id = "activityId" };

        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var responseBody = new Resource() { Id = "activityId" };
        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<string>()
            {
                Body = String.Empty,
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK
            });

        string serviceUrl = "https://serviceurl.com/";
        var activityClient = new ActivityClient(serviceUrl, mockHandler.Object);
        string conversationId = "conversationId";
        var value = new Cards.HeroCard()
        {
            Title = "test card",
            SubTitle = "test fetch config activity"
        };
        var activity = new FetchActivity(value);
        await activityClient.DeleteAsync(conversationId, responseResource.Id);

        string expecteUrl = "https://serviceurl.com/v3/conversations/conversationId/activities/activityId";
        HttpMethod expectedMethod = HttpMethod.Delete;
        mockHandler.Verify(x => x.SendAsync(
            It.Is<IHttpRequest>(arg => arg.Url == expecteUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}