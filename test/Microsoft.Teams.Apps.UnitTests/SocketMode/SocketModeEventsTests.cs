// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.SocketMode;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests.SocketMode;

public class SocketModeEventsTests
{
    [Fact]
    public void ThrowingObservers_AreLoggedWithoutPreventingOtherObservers()
    {
        Mock<ILogger<SocketModeEvents>> logger = new();
        SocketModeEvents events = new(logger.Object);
        InvalidOperationException failure = new("observer failed");
        List<string> observed = [];
        events.Ready += (_, _) => throw failure;
        events.Ready += (_, e) => observed.Add($"ready:{e.Geo}:{e.Frame.ConnectionId}");
        events.Disconnected += (_, _) => throw failure;
        events.Disconnected += (_, e) => observed.Add($"disconnected:{e.Geo}:{e.Terminal}");
        events.Reconnected += (_, _) => throw failure;
        events.Reconnected += (_, e) => observed.Add($"reconnected:{e.Geo}");

        events.OnReady("amer", new SocketReadyFrame { ConnectionId = "connection-1" });
        events.OnDisconnected("amer", null, terminal: false);
        events.OnReconnected("amer");

        Assert.Equal(["ready:amer:connection-1", "disconnected:amer:False", "reconnected:amer"], observed);
        logger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            failure,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(3));
    }
}
