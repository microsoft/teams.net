using System.Net;

using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Common.Http;

using Moq;

namespace Microsoft.Teams.Api.Tests.Clients;

public class MemberClientTests
{
    [Fact]
    public async Task MemberClient_GetAsync()
    {
        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var mockHandler = new Mock<ICustomHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync<List<Account>>(It.IsAny<ICustomHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<List<Account>>()
            {
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK,
                Body = new List<Account>
                {
                    new Account { Id = "member1", Name = "User 1" },
                    new Account { Id = "member2", Name = "User 2" }
                }
            });

        string serviceUrl = "https://serviceurl.com/";
        string conversationId = "conv123";
        var memberClient = new MemberClient(serviceUrl, mockHandler.Object);

        var result = await memberClient.GetAsync(conversationId);

        Assert.Equal(2, result.Count);
        Assert.Equal("member1", result[0].Id);

        string expectedUrl = "https://serviceurl.com/v3/conversations/conv123/members";
        HttpMethod expectedMethod = HttpMethod.Get;
        mockHandler.Verify(x => x.SendAsync<List<Account>>(
            It.Is<ICustomHttpRequest>(arg => arg.Url == expectedUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MemberClient_GetByIdAsync()
    {
        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var mockHandler = new Mock<ICustomHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync<Account>(It.IsAny<ICustomHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<Account>()
            {
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK,
                Body = new Account { Id = "member1", Name = "User 1" }
            });

        string serviceUrl = "https://serviceurl.com/";
        string conversationId = "conv123";
        string memberId = "member1";
        var memberClient = new MemberClient(serviceUrl, mockHandler.Object);

        var result = await memberClient.GetByIdAsync(conversationId, memberId);

        Assert.Equal("member1", result.Id);
        Assert.Equal("User 1", result.Name);

        string expectedUrl = "https://serviceurl.com/v3/conversations/conv123/members/member1";
        HttpMethod expectedMethod = HttpMethod.Get;
        mockHandler.Verify(x => x.SendAsync<Account>(
            It.Is<ICustomHttpRequest>(arg => arg.Url == expectedUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MemberClient_DeleteAsync()
    {
        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var mockHandler = new Mock<ICustomHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync(It.IsAny<ICustomHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<string>()
            {
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.NoContent,
                Body = ""
            });

        string serviceUrl = "https://serviceurl.com/";
        string conversationId = "conv123";
        string memberId = "member1";
        var memberClient = new MemberClient(serviceUrl, mockHandler.Object);

        await memberClient.DeleteAsync(conversationId, memberId);

        string expectedUrl = "https://serviceurl.com/v3/conversations/conv123/members/member1";
        HttpMethod expectedMethod = HttpMethod.Delete;
        mockHandler.Verify(x => x.SendAsync(
            It.Is<ICustomHttpRequest>(arg => arg.Url == expectedUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}