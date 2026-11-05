// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Abstractions;
using Microsoft.Teams.Core.Hosting;
using Microsoft.Teams.Core.Schema;
using Moq;

namespace Microsoft.Teams.Core.UnitTests.Hosting;

/// <summary>
/// How <see cref="BotAuthenticationHandler"/> treats an agentic identity on an outbound Bot Framework request.
/// <para>An app-only token on a request that names an agentic user runs the call as the bot app, which the service rejects with an authorization error that does not point back at the identity.</para>
/// <para>The GUID check is .NET-specific: the agentic user id is passed to the object id (<see cref="Guid"/>) overload of <c>WithAgentUserIdentity</c>, and the other overload takes a UPN instead.</para>
/// </summary>
public class BotAuthenticationHandlerAgenticIdentityTests
{
    private const string AgenticAppId = "d94529f7-d988-4caa-8635-a47201acec74";

    /// <summary>Records which acquisition path was reached and whether the request was sent.</summary>
    private sealed class Recorder
    {
        public int AgenticAcquisitions { get; private set; }

        public int AppOnlyAcquisitions { get; private set; }

        public HttpRequestMessage? SentRequest { get; private set; }

        public HttpMessageInvoker Invoker { get; }

        public Recorder()
        {
            Mock<IAuthorizationHeaderProvider> header = new();

            header
                .Setup(h => h.CreateAuthorizationHeaderAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<AuthorizationHeaderProviderOptions>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    AgenticAcquisitions++;
                    return Task.FromResult("Bearer agent-token");
                });

            header
                .Setup(h => h.CreateAuthorizationHeaderForAppAsync(It.IsAny<string>(), It.IsAny<AuthorizationHeaderProviderOptions>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    AppOnlyAcquisitions++;
                    return Task.FromResult("Bearer app-token");
                });

            BotAuthenticationHandler handler = new(header.Object, NullLogger<BotAuthenticationHandler>.Instance)
            {
                InnerHandler = new StubHandler(request => SentRequest = request)
            };

            Invoker = new HttpMessageInvoker(handler);
        }

        public Task<HttpResponseMessage> SendAsync(string agenticUserId)
        {
            HttpRequestMessage request = new(HttpMethod.Post, "https://smba.trafficmanager.net/teams/v3/conversations");
            request.Options.Set(BotAuthenticationHandler.AgenticIdentityKey, new AgenticIdentity
            {
                AgenticAppId = AgenticAppId,
                AgenticUserId = agenticUserId,
                AgenticAppBlueprintId = "blueprint-id"
            });

            return Invoker.SendAsync(request, CancellationToken.None);
        }
    }

    private sealed class StubHandler(Action<HttpRequestMessage> onSend) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            onSend(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    /// <summary>
    /// A user id that is not a GUID fails the request rather than sending it under the bot app's identity.
    /// </summary>
    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("31e29ddb-e4ce-427e-8bda")]
    [InlineData("31e29ddb e4ce 427e 8bda 1a37eb12d43f")]
    public async Task Throws_WithoutAcquiring_WhenTheAgenticUserIdIsNotAGuid(string userId)
    {
        Recorder recorder = new();

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => recorder.SendAsync(userId));

        // The id is a user identifier, and exception messages reach logs and telemetry.
        Assert.DoesNotContain(userId, ex.Message, StringComparison.Ordinal);

        Assert.Equal(0, recorder.AgenticAcquisitions);
        Assert.Equal(0, recorder.AppOnlyAcquisitions);
        Assert.Null(recorder.SentRequest);
    }

    /// <summary>
    /// The complementary case, so the test above cannot pass by the handler rejecting every agentic identity.
    /// </summary>
    [Fact]
    public async Task AcquiresAgenticToken_WhenTheAgenticUserIdIsAGuid()
    {
        Recorder recorder = new();

        using HttpResponseMessage response = await recorder.SendAsync("31e29ddb-e4ce-427e-8bda-1a37eb12d43f");

        Assert.Equal(1, recorder.AgenticAcquisitions);
        Assert.Equal(0, recorder.AppOnlyAcquisitions);
        Assert.NotNull(recorder.SentRequest);
        Assert.Equal("Bearer", recorder.SentRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("agent-token", recorder.SentRequest.Headers.Authorization?.Parameter);
    }
}
