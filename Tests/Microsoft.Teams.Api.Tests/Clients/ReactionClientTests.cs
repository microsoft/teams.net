using System.Net;

using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Api.Messages;
using Microsoft.Teams.Common.Http;

using Moq;

namespace Microsoft.Teams.Api.Tests.Clients;

public class ReactionClientTests
{
    [Fact]
    public async Task ReactionClient_CreateOrUpdateAsync()
    {
        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<string>()
            {
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK,
                Body = string.Empty
            });

        string serviceUrl = "https://serviceurl.com/";
        var reactionClient = new ReactionClient(serviceUrl, mockHandler.Object);
        string conversationId = "conversationId";
        string activityId = "activityId";
        var reactionType = ReactionType.Like;

        await reactionClient.CreateOrUpdateAsync(conversationId, activityId, reactionType);

        string expectedUrl = "https://serviceurl.com/v3/conversations/conversationId/activities/activityId/reactions/like";
        HttpMethod expectedMethod = HttpMethod.Put;
        mockHandler.Verify(x => x.SendAsync(
            It.Is<IHttpRequest>(arg => arg.Url == expectedUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReactionClient_DeleteAsync()
    {
        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<string>()
            {
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK,
                Body = string.Empty
            });

        string serviceUrl = "https://serviceurl.com/";
        var reactionClient = new ReactionClient(serviceUrl, mockHandler.Object);
        string conversationId = "conversationId";
        string activityId = "activityId";
        var reactionType = ReactionType.Heart;

        await reactionClient.DeleteAsync(conversationId, activityId, reactionType);

        string expectedUrl = "https://serviceurl.com/v3/conversations/conversationId/activities/activityId/reactions/heart";
        HttpMethod expectedMethod = HttpMethod.Delete;
        mockHandler.Verify(x => x.SendAsync(
            It.Is<IHttpRequest>(arg => arg.Url == expectedUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
