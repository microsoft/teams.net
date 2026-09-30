# SignalRClientConnection & SocketModeConnection Walkthrough

> This walkthrough covers the two lowest-level building blocks of the Socket
> Mode transport: `SignalRClientConnection.cs` and `SocketModeConnection.cs`.
> Unlike `GeoSocket` and `SocketModeTransport` (see
> [GeoSocket-Walkthrough.md](./GeoSocket-Walkthrough.md) and
> [SocketModeTransport-Walkthrough.md](./SocketModeTransport-Walkthrough.md)),
> **these two files are already on `main`** today, under
> `src/Microsoft.Teams.Apps/SocketMode/`. See
> [SocketMode-Design.md](./SocketMode-Design.md) for the overall architecture.

These two files exist to answer one question cleanly: *"how do we open a
SignalR connection, and how do higher layers depend on that without coupling
to the real SignalR client?"* `SignalRClientConnection.cs` answers the first
half; `SocketModeConnection.cs` answers the second half.

## `SignalRClientConnection.cs`

### 1. `CreateSignalRClientConnection` delegate

A factory delegate `(Uri url, string accessToken, TimeSpan keepAliveInterval,
TimeSpan serverTimeout) => ISignalRClientConnection`. Its only purpose is to
let callers swap in a fake for tests instead of a real SignalR client.

### 2. `ISignalRClientConnection` interface

The minimal surface Socket Mode needs from a SignalR hub connection:

- `OnActivity(handler)` -- register the handler for inbound activity
  envelopes (returns an optional reply frame).
- `OnReady(handler)` -- register the handler for the `SocketReady` frame.
- `OnClosed(handler)` -- register the handler for terminal closure.
- `StartAsync(cancellationToken)` / `StopAsync(cancellationToken)`.

Nothing here is Socket-Mode-specific business logic -- it is purely an
adapter surface over a `HubConnection`.

### 3. `SignalRClientConnection` -- the adapter itself

This is the **only** place in the whole Socket Mode transport that touches
`Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder`:

```csharp
HubConnection connection = new HubConnectionBuilder()
    .WithUrl(url, options =>
    {
        options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
    })
    .WithKeepAliveInterval(keepAliveInterval)
    .WithServerTimeout(serverTimeout)
    .Build();
```

The rest of the class is bookkeeping around that one `HubConnection`:

- `OnActivity` registers a SignalR hub method handler for `"Activity"` (via
  `_connection.On<SocketActivityEnvelope, SocketReplyFrame?>(...)`) and keeps
  the returned `IDisposable` subscription so it can be cleaned up later.
- `OnReady` does the same for the `"SocketReady"` hub method.
- `OnClosed` wraps the caller's `Action<Exception?>` in a `Func<Exception?,
  Task>` and attaches it to `HubConnection.Closed`, keeping a reference so it
  can be detached on dispose (SignalR's `Closed` event would otherwise keep
  the handler alive past the connection's useful lifetime).
- `StartAsync`/`StopAsync` forward directly to the underlying
  `HubConnection`.
- `DisposeAsync` detaches every `Closed` handler, disposes every `On<...>`
  subscription, then disposes the `HubConnection` itself -- in that order, so
  no stale handler can fire mid-teardown.

**Takeaway:** this class is intentionally thin. It is not reinventing
SignalR; it is exactly the "use `HubConnectionBuilder` directly" approach,
wrapped only enough to be unit-testable via `ISignalRClientConnection`.

## `SocketModeConnection.cs`

This file has **no SignalR code at all**. It defines the seam between
`GeoSocket` (which supervises connections across generations) and whatever
implementation actually opens a socket:

### 1. `ISocketConnection`

The contract `GeoSocket` programs against for a single connection
generation:

- `TokenLifetime` -- the remaining lifetime of the connection's token, when
  known, used by `GeoSocket.ScheduleRefresh` to arm the proactive-rotation
  timer.
- `StartAsync(cancellationToken)` / `StopAsync(cancellationToken)`.

### 2. `ISocketConnectionFactory`

`Create(negotiateUri, handlers) -> ISocketConnection`. `GeoSocket` calls this
once per generation (see `ConnectAsync` in the
[GeoSocket walkthrough](./GeoSocket-Walkthrough.md#3-connectasync----one-connection-attempt)).
The production implementation is `SignalRSocketConnectionFactory`, which
negotiates via `SocketModeNegotiator` and then builds a
`SignalRClientConnection` under the hood.

### 3. `SocketConnectionHandlers`

A plain data holder bundling the three callbacks `GeoSocket` passes to a new
connection:

- `OnActivity` -- dispatches an inbound envelope, returns an optional reply
  frame.
- `OnReady` -- reports the `SocketReady` frame.
- `OnClosed` -- reports closure, with a `bool planned` flag so `GeoSocket`
  can distinguish proactive token rotation from an unexpected drop.

**Takeaway:** `SocketModeConnection.cs` is the interface layer that lets
`GeoSocket` be unit-tested against a fake `ISocketConnection` without a real
network socket, and lets the SignalR-specific implementation
(`SignalRSocketConnection`, the next layer up) be swapped or mocked
independently.

## How the pieces fit together

```
GeoSocket
   │ calls ISocketConnectionFactory.Create(...)
   ▼
SignalRSocketConnectionFactory / SignalRSocketConnection   (one generation's lifecycle: negotiate, readiness gate, TokenLifetime)
   │ builds
   ▼
SignalRClientConnection   (thin adapter: HubConnectionBuilder, subscription/handler bookkeeping)
   │ wraps
   ▼
Microsoft.AspNetCore.SignalR.Client.HubConnection   (the actual built-in SignalR client)
```

`SocketModeConnection.cs`'s interfaces (`ISocketConnection`,
`ISocketConnectionFactory`, `SocketConnectionHandlers`) sit at the top of
that stack as the contract `GeoSocket` depends on, so it never has to know
SignalR exists at all.
