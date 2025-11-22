using System.Net;

using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Common.Http;

using Moq;

using static Microsoft.Teams.Api.Clients.UserTokenClient;

namespace Microsoft.Teams.Api.Tests.Clients;

public class UserTokenClientTests
{
    [Fact]
    public async Task UserTokenClient_GetAsync()
    {
        var tokenRequest = new GetTokenRequest
        {
            UserId = "userId-aad",
            ConnectionName = "connectionName",
            ChannelId = new ChannelId("webchat"),
            Code = "200",
        };

        var responseMessage = new HttpResponseMessage
        {
            Headers = { { "Custom-Header", "HeaderValue" } }
        };

        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync<Token.Response>(It.IsAny<IHttpRequest>(), It.IsAny<AgenticIdentity?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<Token.Response>
            {
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK,
                Body = new Token.Response
                {
                    ConnectionName = "connectionName",
                    Token = "validToken"
                }
            });

        var UserTokenClient = new UserTokenClient(mockHandler.Object, "scope");

        var reqBody = await UserTokenClient.GetAsync(tokenRequest);

        Assert.Equal("validToken", reqBody.Token);

        string expecteUrl = "https://token.botframework.com/api/usertoken/GetToken?userId=userId-aad&connectionName=connectionName&channelId=webchat&code=200";
        mockHandler.Verify(x => x.SendAsync<Token.Response>(It.Is<IHttpRequest>(arg => arg.Url == expecteUrl), It.IsAny<AgenticIdentity?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UserTokenClient_GetAadAsync()
    {
        var aadTokenRequest = new GetAadTokenRequest
        {
            UserId = "userId-aad",
            ConnectionName = "connectionName",
            ChannelId = new ChannelId("webchat"),
            ResourceUrls = ["value1", "value2"],
        };

        var responseMessage = new HttpResponseMessage
        {
            Headers = { { "Custom-Header", "HeaderValue" } }
        };

        IDictionary<string, Token.Response> addTokenResponses = new Dictionary<string, Token.Response>
        {
            {
                "first",
                new Token.Response
                {
                    ConnectionName = "connectionName",
                    Token = "validToken"
                }
            },
            {
                "second",
                new Token.Response
                {
                    ConnectionName = "connectionName",
                    Token = "validToken"
                }
            }
        };

        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync<IDictionary<string, Token.Response>>(It.IsAny<IHttpRequest>(), It.IsAny<AgenticIdentity?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<IDictionary<string, Token.Response>>
            {
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK,
                Body = addTokenResponses
            });

        var UserTokenClient = new UserTokenClient(mockHandler.Object, "scope");

        var reqBody = await UserTokenClient.GetAadAsync(aadTokenRequest);

        Assert.Equal(2, reqBody.Count);

        string expecteUrl = "https://token.botframework.com/api/usertoken/GetAadTokens?userId=userId-aad&connectionName=connectionName&channelId=webchat&resourceUrls%5b0%5d=value1&resourceUrls%5b1%5d=value2";
        mockHandler.Verify(x => x.SendAsync<IDictionary<string, Token.Response>>(It.Is<IHttpRequest>(arg => arg.Url == expecteUrl), It.IsAny<AgenticIdentity?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UserTokenClient_GetStatusAsync()
    {
        var tokenStatusRequest = new GetTokenStatusRequest
        {
            UserId = "userId-aad",
            ChannelId = new ChannelId("webchat"),
            IncludeFilter = "validEntry",
        };

        var responseMessage = new HttpResponseMessage
        {
            Headers = { { "Custom-Header", "HeaderValue" } }
        };

        IList<Token.Status> tokenStatusList = new List<Token.Status>
        {
            new Token.Status
            {
                ChannelId = new ChannelId("webchat"),
                ConnectionName = "connectionName",
                HasToken = true,
                ServiceProviderDisplayName = "validEntry"
            },
            new Token.Status
            {
                ChannelId = new ChannelId("webchat"),
                ConnectionName = "connectionName",
                HasToken = true,
                ServiceProviderDisplayName = "validEntry"
            }
        };

        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync<IList<Token.Status>>(It.IsAny<IHttpRequest>(), It.IsAny<AgenticIdentity?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<IList<Token.Status>>
            {
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK,
                Body = tokenStatusList
            });

        var UserTokenClient = new UserTokenClient(mockHandler.Object, "scope");

        var reqBody = await UserTokenClient.GetStatusAsync(tokenStatusRequest);

        Assert.Equal(2, reqBody.Count);

        string expecteUrl = "https://token.botframework.com/api/usertoken/GetTokenStatus?userId=userId-aad&channelId=webchat&includeFilter=validEntry";
        mockHandler.Verify(x => x.SendAsync<IList<Token.Status>>(It.Is<IHttpRequest>(arg => arg.Url == expecteUrl), It.IsAny<AgenticIdentity?>(), It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task UserTokenClient_SignOutAsync()
    {
        var signOutRequest = new SignOutRequest
        {
            UserId = "userId-aad",
            ChannelId = new ChannelId("msteams"),
            ConnectionName = "connectionName",
        };


        var responseMessage = new HttpResponseMessage
        {
            Headers = { { "Custom-Header", "HeaderValue" } }
        };

        IList<Token.Status> tokenStatusList = new List<Token.Status>
        {
            new Token.Status
            {
                ChannelId = new ChannelId("webchat"),
                ConnectionName = "connectionName",
                HasToken = true,
                ServiceProviderDisplayName = "validEntry"
            },
            new Token.Status
            {
                ChannelId = new ChannelId("webchat"),
                ConnectionName = "connectionName",
                HasToken = true,
                ServiceProviderDisplayName = "validEntry"
            }
        };

        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync(It.IsAny<IHttpRequest>(), It.IsAny<AgenticIdentity?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<string>()
            {
                Body = "valid signin data",
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK
            });


        var UserTokenClient = new UserTokenClient(mockHandler.Object, "scope");

        await UserTokenClient.SignOutAsync(signOutRequest);

        string expecteUrl = "https://token.botframework.com/api/usertoken/SignOut?userId=userId-aad&connectionName=connectionName&channelId=msteams";
        mockHandler.Verify(x => x.SendAsync(It.Is<IHttpRequest>(arg => arg.Url == expecteUrl), It.IsAny<AgenticIdentity?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UserTokenClient_ExchangeAsync()
    {
        var tokenRequest = new ExchangeTokenRequest()
        {
            ChannelId = new ChannelId("msteams"),
            UserId = "userId-aad",
            ConnectionName = "connectionName",
            ExchangeRequest = new TokenExchange.Request()
        };


        var responseMessage = new HttpResponseMessage
        {
            Headers = { { "Custom-Header", "HeaderValue" } }
        };

        var mockHandler = new Mock<IHttpClient>();
        mockHandler
            .Setup(handler => handler.SendAsync<Token.Response>(It.IsAny<IHttpRequest>(), It.IsAny<AgenticIdentity?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponse<Token.Response>
            {
                Headers = responseMessage.Headers,
                StatusCode = HttpStatusCode.OK,
                Body = new Token.Response
                {
                    ConnectionName = "connectionName",
                    Token = "validToken"
                }
            });

        var UserTokenClient = new UserTokenClient(mockHandler.Object, "scope");

        var reqBody = await UserTokenClient.ExchangeAsync(tokenRequest);

        Assert.Equal("validToken", reqBody.Token);
        HttpMethod expectedMethod = HttpMethod.Post;
        string expecteUrl = "https://token.botframework.com/api/usertoken/exchange?userId=userId-aad&connectionName=connectionName&channelId=msteams";
        mockHandler.Verify(x => x.SendAsync<Token.Response>(It.Is<IHttpRequest>(arg => arg.Url == expecteUrl && arg.Method == expectedMethod), It.IsAny<AgenticIdentity?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

}