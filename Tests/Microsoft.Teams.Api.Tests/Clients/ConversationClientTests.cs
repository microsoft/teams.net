using System.Net;

using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Common.Http;

using Moq;

using static Microsoft.Teams.Api.Clients.ConversationClient;

namespace Microsoft.Teams.Api.Tests.Clients;

public class ConversationClientTests
{
    [Fact]
    public async Task ConversationClient_CreateAsync()
    {
        var createRequest = new CreateRequest()
        {
            IsGroup = true,
            Bot = new Account() { Id = "botId" },
            Members = new List<Account>() { new Account() { Id = "userId" } },
            TopicName = "topicName"
        };

        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync<ConversationResource>(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<ConversationResource>()
            {
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK,
                Body = new ConversationResource
                {
                    Id = "conversationId",
                    ServiceUrl = "https://serviceurl.com/"
                }
            });

        string serviceUrl = "https://serviceurl.com/";
        var conversationClient = new ConversationClient(serviceUrl, mockHandler.Object);

        var reqBody = await conversationClient.CreateAsync(createRequest);

        Assert.Equal(serviceUrl, reqBody.ServiceUrl);

        // TODO: confirm end of slash is included in serviceUrl
        string expecteUrl = "https://serviceurl.com/v3/conversations";
        HttpMethod expectedMethod = HttpMethod.Post;
        mockHandler.Verify(x => x.SendAsync<ConversationResource>(
            It.Is<IHttpRequest>(arg => arg.Url == expecteUrl && arg.Method == expectedMethod),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}