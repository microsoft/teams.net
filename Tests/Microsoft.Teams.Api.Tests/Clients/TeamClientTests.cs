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
        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync<Team>(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
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
            It.Is<IHttpRequest>(arg => arg.Url == expectedUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TeamClient_GetConversationsAsync()
    {
        var json = """{"conversations":[{"id":"channel1","name":"General"},{"id":"channel2","name":"Dev Team"}]}""";
        var handler = new MockHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
        var httpClient = new Common.Http.HttpClient(new System.Net.Http.HttpClient(handler));

        string serviceUrl = "https://serviceurl.com/";
        string teamId = "team123";
        var teamClient = new TeamClient(serviceUrl, httpClient);

        var result = await teamClient.GetConversationsAsync(teamId);

        Assert.Equal(2, result.Count);
        Assert.Equal("channel1", result[0].Id);
        Assert.Equal("General", result[0].Name);
        Assert.Equal("channel2", result[1].Id);
        Assert.Equal("Dev Team", result[1].Name);

        Assert.Equal(HttpMethod.Get, handler.LastRequest?.Method);
        Assert.Equal("https://serviceurl.com/v3/teams/team123/conversations", handler.LastRequest?.RequestUri?.ToString());
    }

    [Fact]
    public async Task TeamClient_GetConversationsAsync_EmptyList()
    {
        var json = """{"conversations":[]}""";
        var handler = new MockHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
        var httpClient = new Common.Http.HttpClient(new System.Net.Http.HttpClient(handler));
        var teamClient = new TeamClient("https://serviceurl.com/", httpClient);

        var result = await teamClient.GetConversationsAsync("team123");

        Assert.Empty(result);
    }


    private class MockHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(response);
        }
    }
}