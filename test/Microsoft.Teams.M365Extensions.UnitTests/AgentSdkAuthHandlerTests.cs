// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Microsoft.Agents.Authentication;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.M365Extensions.UnitTests;

public class AgentSdkAuthHandlerTests
{
    [Fact]
    public async Task SendAsync_WithNullRequestUri_SkipsAuthAndForwards()
    {
        var connections = new Mock<IConnections>(MockBehavior.Strict);
        var inner = new RecordingHandler();

        using var handler = new AgentSdkAuthHandler(connections.Object)
        {
            InnerHandler = inner,
        };
        using var invoker = new HttpMessageInvoker(handler);

        // A request with no RequestUri and no BaseAddress: the handler cannot pick a
        // connection or resource, so it must forward without stamping an auth header.
        using var request = new HttpRequestMessage();

        using HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, inner.CallCount);
        Assert.Null(inner.LastRequest?.Headers.Authorization);

        // Strict mock: asserts no connection lookup happened on this path.
        connections.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendAsync_WithAgenticIdentityAndCapableProvider_UsesAgenticUserToken()
    {
        var provider = CreateProvider(appOnlyToken: "app-only-token");
        var agentic = provider.As<IAgenticTokenProvider>();
        agentic
            .Setup(p => p.GetAgenticUserTokenAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("agentic-token");

        RecordingHandler inner = await SendWithAgenticIdentityAsync(
            provider,
            new AgenticIdentity { AgenticAppId = "app-1", AgenticUserId = "user-1", TenantId = "tenant-1" });

        Assert.Equal("agentic-token", inner.LastRequest?.Headers.Authorization?.Parameter);
        agentic.Verify(
            p => p.GetAgenticUserTokenAsync("tenant-1", "app-1", "user-1", It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithAgenticIdentityButNonAgenticProvider_FallsBackToAppOnly()
    {
        // Provider implements only IAccessTokenProvider (no IAgenticTokenProvider).
        var provider = CreateProvider(appOnlyToken: "app-only-token");

        RecordingHandler inner = await SendWithAgenticIdentityAsync(
            provider,
            new AgenticIdentity { AgenticAppId = "app-1", AgenticUserId = "user-1", TenantId = "tenant-1" });

        Assert.Equal("app-only-token", inner.LastRequest?.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task SendAsync_WithIncompleteAgenticIdentity_FallsBackToAppOnly()
    {
        var provider = CreateProvider(appOnlyToken: "app-only-token");
        var agentic = provider.As<IAgenticTokenProvider>();
        agentic
            .Setup(p => p.GetAgenticUserTokenAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("agentic-token");

        // Missing AgenticUserId -> not a complete user-delegated identity.
        RecordingHandler inner = await SendWithAgenticIdentityAsync(
            provider,
            new AgenticIdentity { AgenticAppId = "app-1", TenantId = "tenant-1" });

        Assert.Equal("app-only-token", inner.LastRequest?.Headers.Authorization?.Parameter);
        agentic.Verify(
            p => p.GetAgenticUserTokenAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Mock<IAccessTokenProvider> CreateProvider(string appOnlyToken)
    {
        var settings = new ImmutableConnectionSettings(
            new FakeConnectionSettings { Scopes = ["https://api.botframework.com/.default"] });

        var provider = new Mock<IAccessTokenProvider>();
        provider.Setup(p => p.ConnectionSettings).Returns(settings);
        provider
            .Setup(p => p.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<IList<string>>(), It.IsAny<bool>()))
            .ReturnsAsync(appOnlyToken);
        return provider;
    }

    private static async Task<RecordingHandler> SendWithAgenticIdentityAsync(Mock<IAccessTokenProvider> provider, AgenticIdentity identity)
    {
        var connections = new Mock<IConnections>();
        connections.Setup(c => c.GetDefaultConnection()).Returns(provider.Object);

        var inner = new RecordingHandler();
        using var handler = new AgentSdkAuthHandler(connections.Object)
        {
            InnerHandler = inner,
        };
        using var invoker = new HttpMessageInvoker(handler);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://smba.trafficmanager.net/teams/v3/conversations/x/activities");
        request.Options.Set(new HttpRequestOptionsKey<object?>(BotRequestContext.AgenticIdentityKey), identity);

        using HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return inner;
    }

    private sealed class FakeConnectionSettings : ConnectionSettingsBase
    {
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
