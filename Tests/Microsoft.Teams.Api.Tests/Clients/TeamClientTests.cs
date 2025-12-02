using System.Net;

using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Common.Http;

using Moq;

namespace Microsoft.Teams.Api.Tests.Clients;

public class TeamClientTests
{
    [Fact]
    public async Task TeamClient_GetByIdAsync()
    {
        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var mockHandler = new Mock<ICustomHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync<Team>(It.IsAny<ICustomHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<Team>()
            {
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK,
                Body = new Team { Id = "team123", Name = "Test Team" }
            });

        string serviceUrl = "https://serviceurl.com/";
        string teamId = "team123";
        var teamClient = new TeamClient(serviceUrl, mockHandler.Object);

        var result = await teamClient.GetByIdAsync(teamId);

        Assert.Equal("team123", result.Id);
        Assert.Equal("Test Team", result.Name);

        string expectedUrl = "https://serviceurl.com/v3/teams/team123";
        HttpMethod expectedMethod = HttpMethod.Get;
        mockHandler.Verify(x => x.SendAsync<Team>(
            It.Is<ICustomHttpRequest>(arg => arg.Url == expectedUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TeamClient_GetConversationsAsync()
    {
        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var mockHandler = new Mock<ICustomHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync<List<Channel>>(It.IsAny<ICustomHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<List<Channel>>()
            {
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK,
                Body = new List<Channel>
                {
                    new Channel { Id = "channel1", Name = "General" },
                    new Channel { Id = "channel2", Name = "Dev Team" }
                }
            });

        string serviceUrl = "https://serviceurl.com/";
        string teamId = "team123";
        var teamClient = new TeamClient(serviceUrl, mockHandler.Object);

        var result = await teamClient.GetConversationsAsync(teamId);

        Assert.Equal(2, result.Count);
        Assert.Equal("channel1", result[0].Id);
        Assert.Equal("General", result[0].Name);

        string expectedUrl = "https://serviceurl.com/v3/teams/team123/conversations";
        HttpMethod expectedMethod = HttpMethod.Get;
        mockHandler.Verify(x => x.SendAsync<List<Channel>>(
            It.Is<ICustomHttpRequest>(arg => arg.Url == expectedUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}