using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Http;

using Moq;

namespace Microsoft.Teams.Apps.Tests;

public class AppTests
{
    private readonly string _unexpiredJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMiwiZXhwIjoxOTE2MjM5MDIyfQ.ZTe6TPjyWE8aMo-RAXX6aO1K5VkpMwyxofRQcndwYjQ";
    private readonly string _expiredJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMiwiZXhwIjoxNTE2MjM5MDIzfQ.6dB5kVQtR71r1JDYQqe5Aa1MQoEhCdK4b6ryseopAR0";
    private readonly string _serviceUrl = "https://test.net/";
    
    [Fact]
    public async Task Test_App_Start_GetBotToken_Success()
    {
        // arrange
        var credentials = new Mock<IHttpCredentials>();
        var options = new AppOptions()
        {
            Credentials = credentials.Object,
        };
        var app = new App(options);
        var api = new Mock<ApiClient>(_serviceUrl, CancellationToken.None) { CallBase = true };
        api.Setup(a => a.Bots.Token.GetAsync(It.IsAny<IHttpCredentials>(), It.IsAny<IHttpClient>()))
            .ReturnsAsync(new TokenResponse() { AccessToken = _unexpiredJwt, TokenType = "bot" });
        app.Api = api.Object;

        // act
        await app.Start();

        // assert
        api.Verify(api => api.Bots.Token.GetAsync(It.IsAny<IHttpCredentials>(), It.IsAny<IHttpClient>()), Times.Once);
        Assert.True(app.Token!.ToString() == _unexpiredJwt);
    }

    [Fact]
    public async Task Test_App_Start_GetBotToken_Failure()
    {
        // arrange
        var logger = new Mock<Common.Logging.ILogger>();
        var exception = new Exception("failed to get token");
        logger.Setup(logger => logger.Error(It.IsAny<string?>(), It.IsAny<Exception>()));
        var credentials = new Mock<IHttpCredentials>();
        var options = new AppOptions()
        {
            Credentials = credentials.Object,
            Logger = logger.Object,
        };
        var app = new App(options);
        var api = new Mock<ApiClient>(_serviceUrl, CancellationToken.None) { CallBase = true };
        api.Setup(a => a.Bots.Token.GetAsync(It.IsAny<IHttpCredentials>(), It.IsAny<IHttpClient>()))
            .ThrowsAsync(exception);
        app.Api = api.Object;

        // act
        await app.Start();

        // assert
        logger.Verify(logger => logger.Error("Failed to get bot token on app startup.", exception), Times.Once);
        Assert.Null(app.Token);
    }

    [Fact]
    public async Task Test_App_Start_DoesNot_GetBotToken_WhenNoCredentials()
    {
        // arrange
        var options = new AppOptions()
        {
            Credentials = null,
        };
        var app = new App(options);
        var api = new Mock<ApiClient>(_serviceUrl, CancellationToken.None) { CallBase = true };
        api.Setup(a => a.Bots.Token.GetAsync(It.IsAny<IHttpCredentials>(), It.IsAny<IHttpClient>()))
                    .ReturnsAsync(new TokenResponse() { AccessToken = _unexpiredJwt, TokenType = "bot" });
        app.Api = api.Object;

        // act
        await app.Start();

        // assert
        api.Verify(api => api.Bots.Token.GetAsync(It.IsAny<IHttpCredentials>(), It.IsAny<IHttpClient>()), Times.Never);
        Assert.Null(app.Token);
    }

    [Fact]
    public void Test_App_Client_TokenFactory_GetsToken_IfNotExists()
    {
        // arrange
        var client = new Mock<Common.Http.HttpClient>() { CallBase = true };
        var credentials = new Mock<IHttpCredentials>();
        var options = new AppOptions()
        {
            Client = client.Object,
            Credentials = credentials.Object,
        };
        var app = new App(options);
        var api = new Mock<ApiClient>(_serviceUrl, CancellationToken.None) { CallBase = true };
        api.Setup(a => a.Bots.Token.GetAsync(It.IsAny<IHttpCredentials>(), It.IsAny<IHttpClient>()))
                    .ReturnsAsync(new TokenResponse() { AccessToken = _unexpiredJwt, TokenType = "bot" });
        app.Api = api.Object;

        // act
        Assert.NotNull(client.Object.Options.TokenFactory);
        client.Object.Options.TokenFactory();

        // assert
        api.Verify(api => api.Bots.Token.GetAsync(It.IsAny<IHttpCredentials>(), It.IsAny<IHttpClient>()), Times.Once);
        Assert.True(app.Token!.ToString() == _unexpiredJwt);
    }

    [Fact]
    public void Test_App_Client_TokenFactory_GetsToken_IfExpired()
    {
        // arrange
        var client = new Mock<Common.Http.HttpClient>() { CallBase = true };
        var credentials = new Mock<IHttpCredentials>();
        var options = new AppOptions()
        {
            Client = client.Object,
            Credentials = credentials.Object,
        };
        var app = new App(options);
        app.Token = new JsonWebToken(_expiredJwt);
        credentials.Setup(c => c.Resolve(It.IsAny<IHttpClient>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse() { AccessToken = _unexpiredJwt, TokenType = "bot" });

        // act
        Assert.NotNull(client.Object.Options.TokenFactory);
        client.Object.Options.TokenFactory();

        // assert
        credentials.Verify(c => c.Resolve(It.IsAny<IHttpClient>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(app.Token!.ToString() == _unexpiredJwt);
    }

    [Fact]
    public void Test_App_Client_TokenFactory_DoesNotGetToken_IfValid()
    {
        // arrange
        var client = new Mock<Common.Http.HttpClient>() { CallBase = true };
        var credentials = new Mock<IHttpCredentials>();
        var options = new AppOptions()
        {
            Client = client.Object,
            Credentials = credentials.Object,
        };
        var app = new App(options);
        app.Token = new JsonWebToken(_unexpiredJwt);
        credentials.Setup(c => c.Resolve(It.IsAny<IHttpClient>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResponse() { AccessToken = _unexpiredJwt, TokenType = "bot" });
        var api = new Mock<ApiClient>(_serviceUrl, CancellationToken.None) { CallBase = true };
        api.Setup(a => a.Bots.Token.GetAsync(It.IsAny<IHttpCredentials>(), It.IsAny<IHttpClient>()))
                    .ReturnsAsync(new TokenResponse() { AccessToken = _unexpiredJwt, TokenType = "bot" });
        app.Api = api.Object;

        // act
        Assert.NotNull(client.Object.Options.TokenFactory);
        client.Object.Options.TokenFactory();

        // assert
        credentials.Verify(c => c.Resolve(It.IsAny<IHttpClient>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Never);
        api.Verify(api => api.Bots.Token.GetAsync(It.IsAny<IHttpCredentials>(), It.IsAny<IHttpClient>()), Times.Never);
    }

    [Fact]
    public void Test_App_Client_TokenFactory_DoesNotGetToken_IfNoCredentials()
    {
        // arrange
        var client = new Mock<Common.Http.HttpClient>() { CallBase = true };
        var options = new AppOptions()
        {
            Client = client.Object,
            Credentials = null,
        };
        var app = new App(options);
        var api = new Mock<ApiClient>(_serviceUrl, CancellationToken.None) { CallBase = true };
        api.Setup(a => a.Bots.Token.GetAsync(It.IsAny<IHttpCredentials>(), It.IsAny<IHttpClient>()))
                    .ReturnsAsync(new TokenResponse() { AccessToken = _unexpiredJwt, TokenType = "bot" });
        app.Api = api.Object;

        // act
        Assert.NotNull(client.Object.Options.TokenFactory);
        client.Object.Options.TokenFactory();

        // assert
        api.Verify(api => api.Bots.Token.GetAsync(It.IsAny<IHttpCredentials>(), It.IsAny<IHttpClient>()), Times.Never);
        Assert.Null(app.Token);
    }

    [Fact]
    public void Test_App_Client_CustomTokenFactory()
    {
        // arrange
        var client = new Mock<Common.Http.HttpClient>() { CallBase = true };
        var tokenFactoryInvoked = false;
        IHttpClientOptions.HttpTokenFactory tokenFactory = () =>
        {
            tokenFactoryInvoked = true;
            return null;
        };
        client.Object.Options.TokenFactory = tokenFactory;
        var options = new AppOptions()
        {
            Client = client.Object,
            Credentials = null,
        };
        var app = new App(options);

        // act
        client.Object.Options.TokenFactory();

        // assert
        Assert.True(tokenFactoryInvoked);
    }

    [Fact]
    public async Task Test_App_Process_Should_Call_Middleware()
    {
        // arrange
        var client = new Mock<Common.Http.HttpClient>();
        var app = new App();
        var sender = new Mock<ISenderPlugin>();
        sender.Setup(s => s.CreateStream(It.IsAny<ConversationReference>(), It.IsAny<CancellationToken>())).Returns(new Mock<IStreamer>().Object);
        var token = new Mock<IToken>();
        var activity = new MessageActivity()
        {
            From = new() { Id = "testId" }
        };

        // act
        var middlewareCalled = false;
        app.Use(async (context) =>
        {
            middlewareCalled = true;
            await context.Next();
        });
        await app.Process(sender.Object, token.Object, activity);
        
        // assert
        Assert.True(middlewareCalled);
    }

    [Fact]
    public async Task Test_App_Process_Should_Call_Middlewares_In_Order()
    {
        // arrange
        var client = new Mock<Common.Http.HttpClient>();
        var app = new App();
        var sender = new Mock<ISenderPlugin>();
        sender.Setup(s => s.CreateStream(It.IsAny<ConversationReference>(), It.IsAny<CancellationToken>())).Returns(new Mock<IStreamer>().Object);
        var token = new Mock<IToken>();
        var activity = new MessageActivity()
        {
            From = new() { Id = "testId" }
        };

        // act
        var firstMiddlewareCalled = false;
        var secondMiddlewareCalled = false;
        var middlewaresCalledInOrder = false;
        app.Use(async (context) =>
        {
            firstMiddlewareCalled = true;
            var middleware = await context.Next();
            if ((string?)middleware == "middleware2" && secondMiddlewareCalled)
            {
                middlewaresCalledInOrder = true;
            }

            return null;
        });
        app.Use((context) =>
        {
            secondMiddlewareCalled = true;
            return Task.FromResult((object?)"middleware2");
        });
        await app.Process(sender.Object, token.Object, activity);

        // assert
        Assert.True(middlewaresCalledInOrder);
        Assert.True(secondMiddlewareCalled);
        Assert.True(firstMiddlewareCalled);
    }
}
