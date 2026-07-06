// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.OAuth;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests;

public class OAuthFlowTests
{
    private const string GraphConnection = "graph";
    private const string GitHubConnection = "github";
    private const string TestUserId = "user-1";
    private const string TestChannelId = "msteams";

    // ==================== signin/failure scoping ====================

    [Fact]
    public async Task SignInFailure_OnlyNotifiesFlowWithPendingSignIn()
    {
        // Arrange
        TestHarness harness = CreateHarness(GraphConnection, GitHubConnection);
        bool graphFailureFired = false;
        bool githubFailureFired = false;

        harness.GraphFlow!.OnSignInFailure((_, _, _) => { graphFailureFired = true; return Task.CompletedTask; });
        harness.GitHubFlow!.OnSignInFailure((_, _, _) => { githubFailureFired = true; return Task.CompletedTask; });

        // Initiate sign-in only for Graph (sends OAuthCard -> marks pending)
        SetupSilentTokenReturnsNull(harness.MockUserTokenClient, GraphConnection);
        SetupGetSignInResource(harness.MockUserTokenClient);
        SetupSendActivity(harness);

        Context<MessageActivity> ctx = CreateMessageContext(harness, TestUserId);
        await harness.GraphFlow.SignInAsync(ctx);

        // Act - simulate signin/failure invoke for the same user
        Context<InvokeActivity> failureCtx = CreateInvokeContext(harness, TestUserId);
        SignInFailureValue failureValue = new() { Code = "tokenmissing", Message = "Token acquisition failed." };

        // The route handler filters by HasPendingSignIn, so verify the flags
        Assert.True(harness.GraphFlow.HasPendingSignIn(CreateInvokeContext(harness, TestUserId)));
        Assert.False(harness.GitHubFlow.HasPendingSignIn(CreateInvokeContext(harness, TestUserId)));

        await harness.GraphFlow.HandleSignInFailureAsync(failureCtx, failureValue, CancellationToken.None);

        // Assert - only Graph callback fired
        Assert.True(graphFailureFired);
        Assert.False(githubFailureFired);
    }

    [Fact]
    public async Task SignInFailure_BothFlowsPending_OnlyMostRecentNotified()
    {
        // When both flows have pending sign-ins, only the most recently initiated
        // flow should receive the signin/failure callback (not both).
        TestHarness harness = CreateHarness(GraphConnection, GitHubConnection);
        bool graphFailureFired = false;
        bool githubFailureFired = false;

        harness.GraphFlow!.OnSignInFailure((_, _, _) => { graphFailureFired = true; return Task.CompletedTask; });
        harness.GitHubFlow!.OnSignInFailure((_, _, _) => { githubFailureFired = true; return Task.CompletedTask; });

        // Initiate sign-in for Graph first
        SetupSilentTokenReturnsNull(harness.MockUserTokenClient, GraphConnection);
        SetupSilentTokenReturnsNull(harness.MockUserTokenClient, GitHubConnection);
        SetupGetSignInResource(harness.MockUserTokenClient);
        SetupSendActivity(harness);

        Context<MessageActivity> ctx1 = CreateMessageContext(harness, TestUserId);
        await harness.GraphFlow.SignInAsync(ctx1);

        // Then initiate sign-in for GitHub (most recent)
        Context<MessageActivity> ctx2 = CreateMessageContext(harness, TestUserId);
        await harness.GitHubFlow!.SignInAsync(ctx2);

        // Both flows should report pending
        Assert.True(harness.GraphFlow.HasPendingSignIn(CreateInvokeContext(harness, TestUserId)));
        Assert.True(harness.GitHubFlow.HasPendingSignIn(CreateInvokeContext(harness, TestUserId)));

        // Dispatch signin/failure through the route — should only hit GitHub (most recent)
        InvokeActivity failureActivity = new()
        {
            Name = "signin/failure",
            ChannelId = TestChannelId,
            From = new TeamsChannelAccount { Id = TestUserId },
            Recipient = new TeamsChannelAccount { Id = "bot-id" },
            Conversation = new TeamsConversation { Id = "conv-1" },
            ServiceUrl = new Uri("https://smba.trafficmanager.net/test/"),
        };
        Context<TeamsActivity> failureCtx = new(harness.App, failureActivity);
        await harness.App.Router.DispatchWithReturnAsync(failureCtx);

        // Assert — only GitHub (most recent) callback fired
        Assert.False(graphFailureFired);
        Assert.True(githubFailureFired);
    }

    [Fact]
    public async Task SignInFailure_ClearsPendingSignIn()
    {
        TestHarness harness = CreateHarness(GraphConnection);

        SetupSilentTokenReturnsNull(harness.MockUserTokenClient, GraphConnection);
        SetupGetSignInResource(harness.MockUserTokenClient);
        SetupSendActivity(harness);

        Context<MessageActivity> ctx = CreateMessageContext(harness, TestUserId);
        await harness.GraphFlow!.SignInAsync(ctx);

        Assert.True(harness.GraphFlow.HasPendingSignIn(CreateInvokeContext(harness, TestUserId)));

        // Act
        Context<InvokeActivity> failureCtx = CreateInvokeContext(harness, TestUserId);
        await harness.GraphFlow.HandleSignInFailureAsync(failureCtx, new SignInFailureValue { Code = "invokeerror" }, CancellationToken.None);

        // Assert
        Assert.False(harness.GraphFlow.HasPendingSignIn(CreateInvokeContext(harness, TestUserId)));
    }

    [Fact]
    public async Task TokenExchange_Success_ClearsPendingSignIn()
    {
        TestHarness harness = CreateHarness(GraphConnection);

        SetupSilentTokenReturnsNull(harness.MockUserTokenClient, GraphConnection);
        SetupGetSignInResource(harness.MockUserTokenClient);
        SetupSendActivity(harness);

        Context<MessageActivity> ctx = CreateMessageContext(harness, TestUserId);
        await harness.GraphFlow!.SignInAsync(ctx);

        Assert.True(harness.GraphFlow.HasPendingSignIn(CreateInvokeContext(harness, TestUserId)));

        // Arrange exchange
        harness.MockUserTokenClient
            .Setup(c => c.ExchangeTokenAsync(TestUserId, GraphConnection, TestChannelId, "sso-token", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTokenResult { Token = "access-token", ConnectionName = GraphConnection });

        SignInTokenExchangeValue exchangeValue = new() { Id = "exchange-1", ConnectionName = GraphConnection, Token = "sso-token" };
        Context<InvokeActivity> invokeCtx = CreateInvokeContext(harness, TestUserId);

        // Act
        InvokeResponse response = await harness.GraphFlow.HandleTokenExchangeAsync(invokeCtx, exchangeValue, CancellationToken.None);

        // Assert
        Assert.Equal(200, response.Status);
        Assert.False(harness.GraphFlow.HasPendingSignIn(CreateInvokeContext(harness, TestUserId)));
    }

    [Fact]
    public async Task TokenExchange_Failure_ClearsPendingSignIn()
    {
        TestHarness harness = CreateHarness(GraphConnection);

        SetupSilentTokenReturnsNull(harness.MockUserTokenClient, GraphConnection);
        SetupGetSignInResource(harness.MockUserTokenClient);
        SetupSendActivity(harness);

        Context<MessageActivity> ctx = CreateMessageContext(harness, TestUserId);
        await harness.GraphFlow!.SignInAsync(ctx);

        Assert.True(harness.GraphFlow.HasPendingSignIn(CreateInvokeContext(harness, TestUserId)));

        // Arrange exchange failure
        harness.MockUserTokenClient
            .Setup(c => c.ExchangeTokenAsync(TestUserId, GraphConnection, TestChannelId, "bad-token", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Unauthorized", null, System.Net.HttpStatusCode.Unauthorized));

        SignInTokenExchangeValue exchangeValue = new() { Id = "exchange-2", ConnectionName = GraphConnection, Token = "bad-token" };
        Context<InvokeActivity> invokeCtx = CreateInvokeContext(harness, TestUserId);

        // Act
        InvokeResponse response = await harness.GraphFlow.HandleTokenExchangeAsync(invokeCtx, exchangeValue, CancellationToken.None);

        // Assert - 401 passed through (unexpected code)
        Assert.Equal(401, response.Status);
        Assert.False(harness.GraphFlow.HasPendingSignIn(CreateInvokeContext(harness, TestUserId)));
    }

    [Fact]
    public async Task VerifyState_Success_ClearsPendingSignIn()
    {
        TestHarness harness = CreateHarness(GraphConnection);

        SetupSilentTokenReturnsNull(harness.MockUserTokenClient, GraphConnection);
        SetupGetSignInResource(harness.MockUserTokenClient);
        SetupSendActivity(harness);

        Context<MessageActivity> ctx = CreateMessageContext(harness, TestUserId);
        await harness.GraphFlow!.SignInAsync(ctx);

        Assert.True(harness.GraphFlow.HasPendingSignIn(CreateInvokeContext(harness, TestUserId)));

        // Arrange verify state
        harness.MockUserTokenClient
            .Setup(c => c.GetTokenAsync(TestUserId, GraphConnection, TestChannelId, "123456", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTokenResult { Token = "access-token", ConnectionName = GraphConnection });

        SignInVerifyStateValue verifyValue = new() { State = "123456" };
        Context<InvokeActivity> invokeCtx = CreateInvokeContext(harness, TestUserId);

        // Act
        InvokeResponse response = await harness.GraphFlow.HandleVerifyStateAsync(invokeCtx, verifyValue, CancellationToken.None);

        // Assert
        Assert.Equal(200, response.Status);
        Assert.False(harness.GraphFlow.HasPendingSignIn(CreateInvokeContext(harness, TestUserId)));
    }

    // ==================== No pending sign-in for unrelated user ====================

    [Fact]
    public async Task HasPendingSignIn_FalseForDifferentUser()
    {
        TestHarness harness = CreateHarness(GraphConnection);

        SetupSilentTokenReturnsNull(harness.MockUserTokenClient, GraphConnection);
        SetupGetSignInResource(harness.MockUserTokenClient);
        SetupSendActivity(harness);

        Context<MessageActivity> ctx = CreateMessageContext(harness, TestUserId);
        await harness.GraphFlow!.SignInAsync(ctx);

        Assert.True(harness.GraphFlow.HasPendingSignIn(CreateInvokeContext(harness, TestUserId)));
        Assert.False(harness.GraphFlow.HasPendingSignIn(CreateInvokeContext(harness, "other-user")));
    }

    // ==================== Token exchange error code mapping ====================

    [Fact]
    public async Task TokenExchange_ExpectedError_Returns412WithBody()
    {
        TestHarness harness = CreateHarness(GraphConnection);

        harness.MockUserTokenClient
            .Setup(c => c.ExchangeTokenAsync(TestUserId, GraphConnection, TestChannelId, "sso-token", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Not found", null, System.Net.HttpStatusCode.NotFound));

        SignInTokenExchangeValue exchangeValue = new() { Id = "ex-1", ConnectionName = GraphConnection, Token = "sso-token" };
        Context<InvokeActivity> invokeCtx = CreateInvokeContext(harness, TestUserId);

        InvokeResponse response = await harness.GraphFlow!.HandleTokenExchangeAsync(invokeCtx, exchangeValue, CancellationToken.None);

        Assert.Equal(412, response.Status);
        Assert.NotNull(response.Body);
    }

    [Fact]
    public async Task TokenExchange_UnexpectedError_ReturnsOriginalStatusCode()
    {
        TestHarness harness = CreateHarness(GraphConnection);

        harness.MockUserTokenClient
            .Setup(c => c.ExchangeTokenAsync(TestUserId, GraphConnection, TestChannelId, "sso-token", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Forbidden", null, System.Net.HttpStatusCode.Forbidden));

        SignInTokenExchangeValue exchangeValue = new() { Id = "ex-2", ConnectionName = GraphConnection, Token = "sso-token" };
        Context<InvokeActivity> invokeCtx = CreateInvokeContext(harness, TestUserId);

        InvokeResponse response = await harness.GraphFlow!.HandleTokenExchangeAsync(invokeCtx, exchangeValue, CancellationToken.None);

        Assert.Equal(403, response.Status);
    }

    // ==================== Token exchange deduplication ====================

    [Fact]
    public async Task TokenExchange_Duplicate_Returns200NoOp()
    {
        TestHarness harness = CreateHarness(GraphConnection);

        harness.MockUserTokenClient
            .Setup(c => c.ExchangeTokenAsync(TestUserId, GraphConnection, TestChannelId, "sso-token", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTokenResult { Token = "access-token", ConnectionName = GraphConnection });

        SignInTokenExchangeValue exchangeValue = new() { Id = "dup-1", ConnectionName = GraphConnection, Token = "sso-token" };
        Context<InvokeActivity> invokeCtx = CreateInvokeContext(harness, TestUserId);

        // First call
        InvokeResponse first = await harness.GraphFlow!.HandleTokenExchangeAsync(invokeCtx, exchangeValue, CancellationToken.None);
        Assert.Equal(200, first.Status);

        // Second call with same exchange ID
        InvokeResponse second = await harness.GraphFlow.HandleTokenExchangeAsync(invokeCtx, exchangeValue, CancellationToken.None);
        Assert.Equal(200, second.Status);

        // ExchangeTokenAsync only called once
        harness.MockUserTokenClient.Verify(
            c => c.ExchangeTokenAsync(TestUserId, GraphConnection, TestChannelId, "sso-token", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ==================== verifyState error codes ====================

    [Fact]
    public async Task VerifyState_NullState_Returns404()
    {
        TestHarness harness = CreateHarness(GraphConnection);

        SignInVerifyStateValue verifyValue = new() { State = null };
        Context<InvokeActivity> invokeCtx = CreateInvokeContext(harness, TestUserId);

        InvokeResponse response = await harness.GraphFlow!.HandleVerifyStateAsync(invokeCtx, verifyValue, CancellationToken.None);

        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task VerifyState_NoToken_Returns412_WithoutFiringFailureCallback()
    {
        TestHarness harness = CreateHarness(GraphConnection);
        bool failureFired = false;
        harness.GraphFlow!.OnSignInFailure((_, _, _) => { failureFired = true; return Task.CompletedTask; });

        harness.MockUserTokenClient
            .Setup(c => c.GetTokenAsync(TestUserId, GraphConnection, TestChannelId, "badcode", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetTokenResult?)null);

        SignInVerifyStateValue verifyValue = new() { State = "badcode" };
        Context<InvokeActivity> invokeCtx = CreateInvokeContext(harness, TestUserId);

        InvokeResponse response = await harness.GraphFlow.HandleVerifyStateAsync(invokeCtx, verifyValue, CancellationToken.None);

        Assert.Equal(412, response.Status);
        // No token means the code belongs to another connection — NOT a failure
        Assert.False(failureFired);
    }

    [Fact]
    public async Task VerifyState_ExpectedError_Returns412()
    {
        TestHarness harness = CreateHarness(GraphConnection);

        harness.MockUserTokenClient
            .Setup(c => c.GetTokenAsync(TestUserId, GraphConnection, TestChannelId, "code", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Bad request", null, System.Net.HttpStatusCode.BadRequest));

        SignInVerifyStateValue verifyValue = new() { State = "code" };
        Context<InvokeActivity> invokeCtx = CreateInvokeContext(harness, TestUserId);

        InvokeResponse response = await harness.GraphFlow!.HandleVerifyStateAsync(invokeCtx, verifyValue, CancellationToken.None);

        Assert.Equal(412, response.Status);
    }

    [Fact]
    public async Task VerifyState_UnexpectedError_ReturnsOriginalStatusCode()
    {
        TestHarness harness = CreateHarness(GraphConnection);

        harness.MockUserTokenClient
            .Setup(c => c.GetTokenAsync(TestUserId, GraphConnection, TestChannelId, "code", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Forbidden", null, System.Net.HttpStatusCode.Forbidden));

        SignInVerifyStateValue verifyValue = new() { State = "code" };
        Context<InvokeActivity> invokeCtx = CreateInvokeContext(harness, TestUserId);

        InvokeResponse response = await harness.GraphFlow!.HandleVerifyStateAsync(invokeCtx, verifyValue, CancellationToken.None);

        Assert.Equal(403, response.Status);
    }

    // ==================== signin/failure callback receives failure details ====================

    [Fact]
    public async Task SignInFailure_CallbackReceivesFailureDetails()
    {
        TestHarness harness = CreateHarness(GraphConnection);
        SignInFailureValue? receivedFailure = null;

        harness.GraphFlow!.OnSignInFailure((_, failure, _) => { receivedFailure = failure; return Task.CompletedTask; });

        Context<InvokeActivity> invokeCtx = CreateInvokeContext(harness, TestUserId);
        SignInFailureValue failureValue = new() { Code = "resourcematchfailed", Message = "URI mismatch" };

        await harness.GraphFlow.HandleSignInFailureAsync(invokeCtx, failureValue, CancellationToken.None);

        Assert.NotNull(receivedFailure);
        Assert.Equal("resourcematchfailed", receivedFailure.Code);
        Assert.Equal("URI mismatch", receivedFailure.Message);
    }

    [Fact]
    public async Task TokenExchange_FailureCallback_ReceivesNullFailureValue()
    {
        TestHarness harness = CreateHarness(GraphConnection);
        SignInFailureValue? receivedFailure = new() { Code = "sentinel" };
        bool callbackFired = false;

        harness.GraphFlow!.OnSignInFailure((_, failure, _) =>
        {
            callbackFired = true;
            receivedFailure = failure;
            return Task.CompletedTask;
        });

        harness.MockUserTokenClient
            .Setup(c => c.ExchangeTokenAsync(TestUserId, GraphConnection, TestChannelId, "sso-token", It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Bad request", null, System.Net.HttpStatusCode.BadRequest));

        SignInTokenExchangeValue exchangeValue = new() { Id = "ex-fail", ConnectionName = GraphConnection, Token = "sso-token" };
        Context<InvokeActivity> invokeCtx = CreateInvokeContext(harness, TestUserId);

        await harness.GraphFlow.HandleTokenExchangeAsync(invokeCtx, exchangeValue, CancellationToken.None);

        Assert.True(callbackFired);
        Assert.Null(receivedFailure);
    }

    // ==================== SignInAsync returns token when cached ====================

    [Fact]
    public async Task SignInAsync_WithCachedToken_ReturnsToken()
    {
        TestHarness harness = CreateHarness(GraphConnection);

        harness.MockUserTokenClient
            .Setup(c => c.GetTokenAsync(TestUserId, GraphConnection, TestChannelId, null, It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTokenResult { Token = "cached-token", ConnectionName = GraphConnection });

        Context<MessageActivity> ctx = CreateMessageContext(harness, TestUserId);
        string? token = await harness.GraphFlow!.SignInAsync(ctx);

        Assert.Equal("cached-token", token);
        Assert.False(harness.GraphFlow.HasPendingSignIn(CreateInvokeContext(harness, TestUserId)));
    }

    [Fact]
    public async Task SignInAsync_NoToken_SendsOAuthCardAndReturnsNull()
    {
        TestHarness harness = CreateHarness(GraphConnection);

        SetupSilentTokenReturnsNull(harness.MockUserTokenClient, GraphConnection);
        SetupGetSignInResource(harness.MockUserTokenClient);
        SetupSendActivity(harness);

        Context<MessageActivity> ctx = CreateMessageContext(harness, TestUserId);
        string? token = await harness.GraphFlow!.SignInAsync(ctx);

        Assert.Null(token);
        Assert.True(harness.GraphFlow.HasPendingSignIn(CreateInvokeContext(harness, TestUserId)));
    }

    // ==================== Helpers ====================

    private sealed class TestHarness
    {
        public required TeamsBotApplication App { get; init; }
        public required Mock<UserTokenClient> MockUserTokenClient { get; init; }
        public required Mock<ConversationClient> MockConversationClient { get; init; }
        public OAuthFlow? GraphFlow { get; init; }
        public OAuthFlow? GitHubFlow { get; init; }
    }

    private static TestHarness CreateHarness(params string[] connectionNames)
    {
        Mock<UserTokenClient> mockUserTokenClient = CreateMockUserTokenClient();
        Mock<ConversationClient> mockConversationClient = new(new HttpClient(), NullLogger<ConversationClient>.Instance);

        ApiClient apiClient = new(
            new HttpClient(),
            mockConversationClient.Object,
            mockUserTokenClient.Object);

        TeamsBotApplication app = new(
            apiClient,
            new HttpContextAccessor(),
            NullLogger<TeamsBotApplication>.Instance,
            new TeamsBotApplicationOptions { AppId = "test-app-id" });

        OAuthFlow? graphFlow = null;
        OAuthFlow? githubFlow = null;

        foreach (string name in connectionNames)
        {
            OAuthFlow flow = app.AddOAuthFlow(name);
            if (name == GraphConnection) graphFlow = flow;
            else if (name == GitHubConnection) githubFlow = flow;
        }

        return new TestHarness
        {
            App = app,
            MockUserTokenClient = mockUserTokenClient,
            MockConversationClient = mockConversationClient,
            GraphFlow = graphFlow,
            GitHubFlow = githubFlow
        };
    }

    private static Mock<UserTokenClient> CreateMockUserTokenClient()
    {
        Mock<IConfiguration> mockConfig = new();
        return new Mock<UserTokenClient>(
            new HttpClient(),
            mockConfig.Object,
            NullLogger<UserTokenClient>.Instance);
    }

    private static Context<MessageActivity> CreateMessageContext(TestHarness harness, string userId)
    {
        MessageActivity activity = new("hello")
        {
            ChannelId = TestChannelId,
            From = new TeamsChannelAccount { Id = userId },
            Recipient = new TeamsChannelAccount { Id = "bot-id" },
            Conversation = new TeamsConversation { Id = "conv-1" },
            ServiceUrl = new Uri("https://smba.trafficmanager.net/test/"),
        };

        return new Context<MessageActivity>(harness.App, activity);
    }

    private static Context<InvokeActivity> CreateInvokeContext(TestHarness harness, string userId)
    {
        InvokeActivity activity = new()
        {
            ChannelId = TestChannelId,
            From = new TeamsChannelAccount { Id = userId },
            Recipient = new TeamsChannelAccount { Id = "bot-id" },
            Conversation = new TeamsConversation { Id = "conv-1" },
            ServiceUrl = new Uri("https://smba.trafficmanager.net/test/"),
        };

        return new Context<InvokeActivity>(harness.App, activity);
    }

    private static void SetupSilentTokenReturnsNull(Mock<UserTokenClient> mock, string connectionName)
    {
        mock.Setup(c => c.GetTokenAsync(TestUserId, connectionName, TestChannelId, null, It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetTokenResult?)null);
    }

    private static void SetupGetSignInResource(Mock<UserTokenClient> mock)
    {
        mock.Setup(c => c.GetSignInResourceAsync(It.IsAny<string>(), null, (Uri?)null, (Uri?)null, It.IsAny<BotRequestContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSignInResourceResult
            {
                SignInLink = "https://login.microsoftonline.com/test",
                TokenExchangeResource = new TokenExchangeResource { Id = "tex-1", Uri = new Uri("api://test") },
                TokenPostResource = new TokenPostResource { SasUrl = new Uri("https://token.botframework.com/test") }
            });
    }

    private static void SetupSendActivity(TestHarness harness)
    {
        harness.MockConversationClient
            .Setup(c => c.SendActivityAsync(It.IsAny<CoreActivity>(), It.IsAny<BotRequestContext?>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendActivityResponse { Id = "activity-1" });
    }
}
