using System.Net;

using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Common.Http;

using Moq;

namespace Microsoft.Teams.Api.Tests.Clients;

public class BotSignInClientTests
{
    [Fact]
    public async Task BotSignInClient_GetUrlAsync_Async()
    {
        var getUrlRequest = new BotSignInClient.GetUrlRequest()
        {
            State = "state",
        };
        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<string>()
            {
                Body = "valid signin data",
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK
            });

        var botSignInClient = new BotSignInClient(mockHandler.Object);

        var reqBody = await botSignInClient.GetUrlAsync(getUrlRequest);

        Assert.Equal("valid signin data", reqBody);

        string expecteUrl = "https://token.botframework.com/api/botsignin/GetSignInUrl?State=state&CodeChallenge=&EmulatorUrl=&FinalRedirect=";
        mockHandler.Verify(x => x.SendAsync(It.Is<IHttpRequest>(arg => arg.Url == expecteUrl), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BotSignInClient_GetUrlAsync_UrlRequest_Async()
    {
        var getUrlRequest = new BotSignInClient.GetUrlRequest()
        {
            State = "state",
            CodeChallenge = "code$1",
            EmulatorUrl = "https://emulator.com",
            FinalRedirect = "https://somewhere.com"
        };
        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");


        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<string>()
            {
                Body = "valid signin data",
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK
            });

        var botSignInClient = new BotSignInClient(mockHandler.Object);

        var reqBody = await botSignInClient.GetUrlAsync(getUrlRequest);

        Assert.Equal("valid signin data", reqBody);

        string expecteUrl = "https://token.botframework.com/api/botsignin/GetSignInUrl?State=state&CodeChallenge=code%241&EmulatorUrl=https%3a%2f%2femulator.com&FinalRedirect=https%3a%2f%2fsomewhere.com";
        mockHandler.Verify(x => x.SendAsync(It.Is<IHttpRequest>(arg => arg.Url == expecteUrl), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BotSignInClient_GetResourceAsync_Async()
    {
        var getUrlRequest = new BotSignInClient.GetResourceRequest()
        {
            State = "state",
        };
        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");


        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync<SignIn.UrlResponse>(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<SignIn.UrlResponse>()
            {
                Body = new SignIn.UrlResponse()
                {
                    SignInLink = "valid signin data"
                },
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK
            });


        var botSignInClient = new BotSignInClient(mockHandler.Object);

        var reqBody = await botSignInClient.GetResourceAsync(getUrlRequest);

        Assert.Equal("valid signin data", reqBody.SignInLink);

        string expecteUrl = "https://token.botframework.com/api/botsignin/GetSignInResource?State=state&CodeChallenge=&EmulatorUrl=&FinalRedirect=";
        mockHandler.Verify(x => x.SendAsync<SignIn.UrlResponse>(It.Is<IHttpRequest>(arg => arg.Url == expecteUrl), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BotSignInClient_GetResourceAsync_RequestParams_Async()
    {
        var getUrlRequest = new BotSignInClient.GetResourceRequest()
        {
            State = "state",
            CodeChallenge = "code$1",
            EmulatorUrl = "https://emulator.com",
            FinalRedirect = "https://somewhere.com",
        };
        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");


        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync<SignIn.UrlResponse>(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<SignIn.UrlResponse>()
            {
                Body = new SignIn.UrlResponse()
                {
                    SignInLink = "valid signin data"
                },
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK
            });


        var botSignInClient = new BotSignInClient(mockHandler.Object);

        var reqBody = await botSignInClient.GetResourceAsync(getUrlRequest);

        Assert.Equal("valid signin data", reqBody.SignInLink);

        string expecteUrl = "https://token.botframework.com/api/botsignin/GetSignInResource?State=state&CodeChallenge=code%241&EmulatorUrl=https%3a%2f%2femulator.com&FinalRedirect=https%3a%2f%2fsomewhere.com";
        mockHandler.Verify(x => x.SendAsync<SignIn.UrlResponse>(It.Is<IHttpRequest>(arg => arg.Url == expecteUrl), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BotSignInClient_GetUrlAsync_WithRegionalEndpoint()
    {
        var apiClientSettings = new ApiClientSettings("https://europe.token.botframework.com");
        var getUrlRequest = new BotSignInClient.GetUrlRequest()
        {
            State = "state",
        };
        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");
        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<string>()
            {
                Body = "valid signin data",
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK
            });

        var botSignInClient = new BotSignInClient(mockHandler.Object, apiClientSettings);

        var reqBody = await botSignInClient.GetUrlAsync(getUrlRequest);

        Assert.Equal("valid signin data", reqBody);

        string expectedUrl = "https://europe.token.botframework.com/api/botsignin/GetSignInUrl?State=state&CodeChallenge=&EmulatorUrl=&FinalRedirect=";
        mockHandler.Verify(x => x.SendAsync(It.Is<IHttpRequest>(arg => arg.Url == expectedUrl), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BotSignInClient_GetResourceAsync_WithRegionalEndpoint()
    {
        var apiClientSettings = new ApiClientSettings("https://europe.token.botframework.com");
        var getUrlRequest = new BotSignInClient.GetResourceRequest()
        {
            State = "state",
            CodeChallenge = "code$1",
            EmulatorUrl = "https://emulator.com",
            FinalRedirect = "https://somewhere.com",
        };
        var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Custom-Header", "HeaderValue");

        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync<SignIn.UrlResponse>(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<SignIn.UrlResponse>()
            {
                Body = new SignIn.UrlResponse()
                {
                    SignInLink = "valid signin data"
                },
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK
            });

        var botSignInClient = new BotSignInClient(mockHandler.Object, apiClientSettings);

        var reqBody = await botSignInClient.GetResourceAsync(getUrlRequest);

        Assert.Equal("valid signin data", reqBody.SignInLink);

        string expectedUrl = "https://europe.token.botframework.com/api/botsignin/GetSignInResource?State=state&CodeChallenge=code%241&EmulatorUrl=https%3a%2f%2femulator.com&FinalRedirect=https%3a%2f%2fsomewhere.com";
        mockHandler.Verify(x => x.SendAsync<SignIn.UrlResponse>(It.Is<IHttpRequest>(arg => arg.Url == expectedUrl), It.IsAny<CancellationToken>()), Times.Once);
    }
}