// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Teams.Bot.Core.Hosting;
using Microsoft.Teams.Bot.Core.Schema;
using Moq;

namespace Microsoft.Teams.Bot.Core.UnitTests.Hosting;

public class BotAuthenticationHandlerTests
{
    private static BotAuthenticationHandler CreateHandler(
        Mock<IAuthorizationHeaderProvider> providerMock,
        string scope = "https://api.botframework.com/.default")
    {
        return new BotAuthenticationHandler(
            providerMock.Object,
            NullLogger<BotAuthenticationHandler>.Instance,
            scope,
            managedIdentityOptions: null);
    }

    [Fact]
    public async Task SendAsync_WithValidAgenticUserId_AcquiresAgenticToken()
    {
        // Arrange
        Mock<IAuthorizationHeaderProvider> providerMock = new();
        providerMock
            .Setup(p => p.CreateAuthorizationHeaderAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<AuthorizationHeaderProviderOptions>(),
                It.IsAny<ClaimsPrincipal?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Bearer agentic-token");

        BotAuthenticationHandler handler = CreateHandler(providerMock);
        handler.InnerHandler = new SuccessHandler();

        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com");
        request.Options.Set(BotAuthenticationHandler.AgenticIdentityKey, new AgenticIdentity
        {
            AgenticAppId = "app-id-123",
            AgenticUserId = "00000000-0000-0000-0000-000000000001"  // valid GUID
        });

        using HttpMessageInvoker invoker = new(handler);

        // Act
        HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert – agentic provider was called
        Assert.True(response.IsSuccessStatusCode);
        providerMock.Verify(p => p.CreateAuthorizationHeaderAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<AuthorizationHeaderProviderOptions>(),
            It.IsAny<ClaimsPrincipal?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithMalformedAgenticUserId_FallsBackToAppOnlyToken()
    {
        // Arrange – malformed GUID should NOT throw; handler falls back to app-only
        Mock<IAuthorizationHeaderProvider> providerMock = new();
        providerMock
            .Setup(p => p.CreateAuthorizationHeaderForAppAsync(
                It.IsAny<string>(),
                It.IsAny<AuthorizationHeaderProviderOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Bearer app-only-token");

        BotAuthenticationHandler handler = CreateHandler(providerMock);
        handler.InnerHandler = new SuccessHandler();

        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com");
        request.Options.Set(BotAuthenticationHandler.AgenticIdentityKey, new AgenticIdentity
        {
            AgenticAppId = "app-id-123",
            AgenticUserId = "not-a-guid"   // malformed — must NOT throw FormatException
        });

        using HttpMessageInvoker invoker = new(handler);

        // Act – must succeed without throwing
        HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert – fell back to app-only
        Assert.True(response.IsSuccessStatusCode);
        providerMock.Verify(p => p.CreateAuthorizationHeaderForAppAsync(
            It.IsAny<string>(),
            It.IsAny<AuthorizationHeaderProviderOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
        // Agentic path must NOT have been taken
        providerMock.Verify(p => p.CreateAuthorizationHeaderAsync(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<AuthorizationHeaderProviderOptions>(),
            It.IsAny<ClaimsPrincipal?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_WithMalformedJwtToken_DoesNotCrash()
    {
        // Arrange – LogTokenClaims should swallow parse errors, never crashing the pipeline
        Mock<IAuthorizationHeaderProvider> providerMock = new();
        providerMock
            .Setup(p => p.CreateAuthorizationHeaderForAppAsync(
                It.IsAny<string>(),
                It.IsAny<AuthorizationHeaderProviderOptions>(),
                It.IsAny<CancellationToken>()))
            // Return a non-JWT opaque token (e.g. managed identity token)
            .ReturnsAsync("Bearer not.a.valid.jwt.token.here");

        BotAuthenticationHandler handler = CreateHandler(providerMock);
        handler.InnerHandler = new SuccessHandler();

        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com");
        // No agentic identity — goes to app-only path
        using HttpMessageInvoker invoker = new(handler);

        // Act – must not throw even though the token can't be parsed for claim logging
        HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    // Minimal inner handler that always returns 200 OK.
    private sealed class SuccessHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}
