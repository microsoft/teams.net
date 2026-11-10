// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Apps.SocketMode;

namespace Microsoft.Teams.Apps.UnitTests.SocketMode;

public class SocketModeObserverTests
{
    [Fact]
    public async Task Diagnostics_AreLazyAndDoNotConnect()
    {
        int created = 0;
        SocketModeEvents events = new(NullLogger<SocketModeEvents>.Instance);
        await using SocketModeTransport transport = new(
            new SocketModeTransportOptions
            {
                Geos = ["emea"],
            },
            new UnusedConnectionFactory(),
            _ => Task.FromResult(new SocketDispatchResult(200)),
            NullLogger.Instance);
        SocketModeObserver observer = new(() =>
        {
            created++;
            return transport;
        }, events);

        Assert.Same(events, observer.Events);
        observer.Events.Ready += (_, _) => { };
        Assert.Equal(0, created);
        Assert.Equal(SocketModeStatus.Idle, observer.Status);
        Assert.Equal(new Uri("https://emea.botapi.skype.com/v3/websockets/connect"), observer.NegotiateUrl);
        Assert.Equal(["emea"], observer.GeoList);
        Assert.Empty(observer.GeoStatuses);
        Assert.Equal(1, created);
    }

    private sealed class UnusedConnectionFactory : ISocketConnectionFactory
    {
        public ISocketConnection Create(
            Uri negotiateUri,
            SocketConnectionHandlers handlers,
            Func<string?>? getReplacedConnectionId = null)
            => throw new InvalidOperationException("Reading diagnostics must not connect.");
    }
}
