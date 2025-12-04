using System.Net;

using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Api.Meetings;
using Microsoft.Teams.Common.Http;

using Moq;

namespace Microsoft.Teams.Api.Tests.Clients;

public class MeetingClientTests
{
    [Fact]
    public async Task MeetingClient_GetByIdAsync()
    {
        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync<Meeting>(It.IsAny<IHttpRequest>(), It.IsAny<AgenticIdentity?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<Meeting>()
            {
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK,
                Body = new Meeting { Id = "meeting123" }
            });

        string serviceUrl = "https://serviceurl.com/";
        string meetingId = "meeting123";
        var meetingClient = new MeetingClient(serviceUrl, mockHandler.Object, "scope");

        var result = await meetingClient.GetByIdAsync(meetingId);

        Assert.Equal("meeting123", result.Id);

        string expectedUrl = "https://serviceurl.com/v1/meetings/meeting123";
        HttpMethod expectedMethod = HttpMethod.Get;
        mockHandler.Verify(x => x.SendAsync<Meeting>(
            It.Is<IHttpRequest>(arg => arg.Url == expectedUrl && arg.Method == expectedMethod),
            It.IsAny<AgenticIdentity?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MeetingClient_GetParticipantAsync()
    {
        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync<MeetingParticipant>(It.IsAny<IHttpRequest>(), It.IsAny<AgenticIdentity?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<MeetingParticipant>()
            {
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK,
                Body = new MeetingParticipant
                {
                    Id = "participant1",
                    User = new Account { Id = "user1", Name = "John Doe" },
                    Role = "Presenter",
                    IsOrganizer = true,
                    JoinTime = DateTime.UtcNow
                }
            });

        string serviceUrl = "https://serviceurl.com/";
        string meetingId = "meeting123";
        string participantId = "participant1";
        var meetingClient = new MeetingClient(serviceUrl, mockHandler.Object, "scope");

        var result = await meetingClient.GetParticipantAsync(meetingId, participantId);

        Assert.Equal("participant1", result.Id);
        Assert.Equal("user1", result.User?.Id);
        Assert.Equal("John Doe", result.User?.Name);
        Assert.Equal("Presenter", result.Role);
        Assert.True(result.IsOrganizer);

        string expectedUrl = "https://serviceurl.com/v1/meetings/meeting123/participants/participant1";
        HttpMethod expectedMethod = HttpMethod.Get;
        mockHandler.Verify(x => x.SendAsync<MeetingParticipant>(
            It.Is<IHttpRequest>(arg => arg.Url == expectedUrl && arg.Method == expectedMethod),
            It.IsAny<AgenticIdentity?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}