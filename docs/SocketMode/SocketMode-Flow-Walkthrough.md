# Socket Mode Flow Walkthrough

> This is a single, flow-oriented walkthrough of the Socket Mode transport,
> replacing three earlier per-class walkthrough docs. Instead of going
> class-by-class, it follows four concrete sequences end-to-end: **startup**,
> **steady-state dispatch**, **make-before-break token rotation**, and
> **unexpected disconnect / reconnect**. See
> [SocketMode-Design.md](./SocketMode-Design.md) for the user-facing design
> and options.
>
> `SignalRClientConnection`, `SignalRSocketConnection`/
> `SignalRSocketConnectionFactory`, and `SocketModeConnection.cs`
> (`ISocketConnection`, `ISocketConnectionFactory`, `SocketConnectionHandlers`)
> are already on `main`. `GeoSocket` and `SocketModeTransport` are still
> landing via [PR #686](https://github.com/microsoft/teams.net/pull/686).

## Component map

```
SocketModeHostedService        (IHostedService; blocks host startup until every geo is ready)
        │
        ▼
SocketModeTransport            (implements IGeoSocketOwner; one GeoSocket per configured geo)
        │
        ▼
GeoSocket                      (per-geo supervisor: generations, rotation, retry/backoff)
        │ ISocketConnectionFactory.Create(...)
        ▼
SignalRSocketConnectionFactory / SignalRSocketConnection
        │ negotiates via SocketModeNegotiator, then builds
        ▼
SignalRClientConnection        (thin adapter over HubConnectionBuilder)
        │
        ▼
Microsoft.AspNetCore.SignalR.Client.HubConnection
```

`ISocketConnection` / `ISocketConnectionFactory` / `SocketConnectionHandlers`
(all in `SocketModeConnection.cs`) are the interfaces that let `GeoSocket`
depend on "some connection generation" without knowing SignalR exists.

## 1. Startup

`SocketModeHostedService` doesn't finish starting until **every** geo has a
ready connection (within `StartupTimeout`, default 30s). A failure here
surfaces from `host.Run()`.

```mermaid
sequenceDiagram
    participant Host as SocketModeHostedService
    participant Transport as SocketModeTransport
    participant Geo as GeoSocket (per geo)
    participant Factory as SignalRSocketConnectionFactory
    participant Conn as SignalRSocketConnection
    participant Neg as SocketModeNegotiator
    participant Client as SignalRClientConnection
    participant Hub as HubConnection

    Host->>Transport: StartAsync()
    par for each configured geo
        Transport->>Geo: StartAsync()
        Geo->>Geo: ConnectAsync() (generation 1)
        Geo->>Factory: Create(negotiateUri, handlers)
        Factory->>Conn: new SignalRSocketConnection
        Geo->>Conn: StartAsync()
        Conn->>Neg: NegotiateAsync(negotiateUri)
        Neg-->>Conn: url, accessToken, expiresIn
        Conn->>Client: CreateSignalRClientConnection(url, token, keepAlive, serverTimeout)
        Conn->>Client: OnActivity / OnReady / OnClosed
        Conn->>Client: StartAsync()
        Client->>Hub: HubConnectionBuilder...Build().StartAsync()
        Hub-->>Client: SocketReady frame
        Client-->>Conn: OnReady(frame)
        Conn-->>Conn: TokenLifetime set, readySource resolved
        Conn-->>Geo: StartAsync() returns
        Geo->>Geo: TryPromote(generation 1) -> active
        Geo-->>Transport: geo ready
    end
    Transport-->>Host: all geos ready -> StartAsync() returns
```

If a geo's `ConnectAsync` fails to reach ready within `StartupTimeout` (e.g.
negotiate 401/403, or `ReadinessTimeout` elapses waiting for `SocketReady`),
that failure propagates up through `Transport.StartAsync()` and
`Host.StartAsync()`, so the whole host fails to start rather than running
with a silently-missing geo.

## 2. Steady-state dispatch

Once a generation is active, inbound activity envelopes flow straight
through to the existing `TeamsBotApplication` pipeline -- there is no
parallel activity-processing framework.

```mermaid
sequenceDiagram
    participant Hub as HubConnection
    participant Client as SignalRClientConnection
    participant Conn as SignalRSocketConnection
    participant Geo as GeoSocket
    participant Transport as SocketModeTransport
    participant App as TeamsBotApplication

    Hub->>Client: "Activity" hub invocation (envelope)
    Client->>Conn: OnActivity handler
    Conn->>Conn: await readySource.Task (already resolved)
    Conn->>Geo: handlers.OnActivity(envelope)
    Geo->>Geo: only the active (non-retiring) generation dispatches
    Geo->>Transport: DispatchAsync(envelope)
    Transport->>App: ProcessWithInvokeResponseAsync(activity, user: null, correlationVector: null)
    App-->>Transport: invoke response (status + body) via AsyncLocal capture
    Transport-->>Geo: reply frame
    Geo-->>Conn: reply frame
    Conn-->>Client: return value from OnActivity handler
    Client-->>Hub: reply frame returned to server
```

No per-activity principal is attached -- the socket connection itself is
already authenticated with the bot's app token. The invoke response is
captured via an `AsyncLocal` object instead of `HttpContext`, since there is
no HTTP context in Socket Mode.

## 3. Make-before-break token rotation

Connections are rotated **before** their token expires (`TokenRefreshMargin`
before `TokenLifetime` runs out), so there's no gap where the active
connection is unauthenticated.

```mermaid
sequenceDiagram
    participant Geo as GeoSocket
    participant NewConn as Generation N+1
    participant OldConn as Generation N (retiring)

    Note over Geo: ScheduleRefresh timer fires at TokenLifetime - TokenRefreshMargin
    Geo->>Geo: RequestRotation()
    Geo->>NewConn: ConnectAsync() (same negotiate/connect/ready sequence as startup)
    NewConn-->>Geo: ready
    Geo->>Geo: TryPromote(N+1) -> N+1 becomes active
    Geo->>OldConn: StartRetirement(N)
    Note over OldConn: N keeps dispatching for up to HandoffWindow
    Geo->>OldConn: RetireAsync(N) once HandoffWindow elapses
    OldConn->>OldConn: StopAsync() / DisposeAsync()
```

During the handoff window there are briefly **two** live generations for the
same geo: the new one is already active for new dispatch, while the old one
finishes out its window so in-flight requests aren't dropped mid-rotation.

## 4. Unexpected disconnect & reconnect

Unplanned closures (network blips, server-initiated drops) are distinguished
from planned ones (rotation, shutdown) via the `planned` flag on
`OnClosed(error, planned)`.

```mermaid
sequenceDiagram
    participant Hub as HubConnection
    participant Client as SignalRClientConnection
    participant Conn as SignalRSocketConnection
    participant Geo as GeoSocket
    participant Transport as SocketModeTransport

    Hub--xClient: connection drops unexpectedly
    Client-->>Conn: HubConnection.Closed
    Conn->>Conn: HandleClosed(error) -- planned = (_stopped != 0) = false
    Conn-->>Geo: handlers.OnClosed(error, planned: false)
    Geo->>Geo: SuperviseAsync: compute delay (ReconnectDelays schedule, or capped exponential backoff + jitter)
    Geo->>Geo: after delay, ConnectAsync() (next generation)
    alt negotiate returns 401/403
        Geo->>Geo: SocketModeNegotiateException.IsNonRetryable = true
        Geo->>Geo: stop retrying this geo permanently (until app restart)
        Geo-->>Transport: OnGeoDisconnected(geo)
    else negotiate/connect succeeds
        Geo->>Geo: TryPromote(new generation) -> active again
    end
```

Each geo is isolated: one geo permanently failing (non-retryable auth
failure) does not affect the others, which keep running and reconnecting
independently.

## Class responsibilities (quick reference)

| Class | Responsibility | Status |
|---|---|---|
| `SocketModeHostedService` | `IHostedService`; blocks host startup until every geo is ready | PR #686 |
| `SocketModeTransport` | Implements `IGeoSocketOwner`; owns all `GeoSocket`s; dispatches to `TeamsBotApplication` | PR #686 |
| `GeoSocket` | Per-geo supervisor: generations, make-before-break rotation, retry/backoff, non-retryable failure isolation | PR #686 |
| `SignalRSocketConnectionFactory` / `SignalRSocketConnection` | One connection generation's lifecycle: negotiate, readiness gate, `TokenLifetime` | `main` |
| `SignalRClientConnection` | Thin adapter over `HubConnectionBuilder`; the only class that touches it directly | `main` |
| `SocketModeConnection.cs` (`ISocketConnection`, `ISocketConnectionFactory`, `SocketConnectionHandlers`) | Interface seam between `GeoSocket` and the SignalR-backed implementation; no SignalR code | `main` |
| `SocketModeNegotiator` | Negotiates SignalR connection info against the negotiate HTTP endpoint | `main` |
