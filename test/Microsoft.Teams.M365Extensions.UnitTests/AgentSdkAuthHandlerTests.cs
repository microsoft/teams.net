// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Microsoft.Agents.Authentication;
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
