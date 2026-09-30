# SignalRClientConnection, SignalRSocketConnection & SocketModeConnection Walkthrough

> This walkthrough covers the three lowest-level building blocks of the
> Socket Mode transport: `SignalRClientConnection.cs`,
> `SignalRSocketConnection.cs`, and `SocketModeConnection.cs`. Unlike
> `GeoSocket` and `SocketModeTransport` (see
> [GeoSocket-Walkthrough.md](./GeoSocket-Walkthrough.md) and
> [SocketModeTransport-Walkthrough.md](./SocketModeTransport-Walkthrough.md)),
> **these three files are already on `main`** today, under
> `src/Microsoft.Teams.Apps/SocketMode/`. See
> [SocketMode-Design.md](./SocketMode-Design.md) for the overall architecture.

These three files exist to answer one question cleanly: *"how do we open a
SignalR connection, manage one connection generation's lifecycle, and let
higher layers depend on that without coupling to the real SignalR client?"*
`SignalRClientConnection.cs` answers the first part, `SignalRSocketConnection.cs`
answers the second, and `SocketModeConnection.cs` answers the third.

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

## `SignalRSocketConnection.cs`

This file sits directly above `SignalRClientConnection.cs` and implements
`ISocketConnection`/`ISocketConnectionFactory` from `SocketModeConnection.cs`
for the **real, SignalR-backed** case. Where `SignalRClientConnection` only
knows how to talk to one already-negotiated `HubConnection`,
`SignalRSocketConnection` is responsible for everything needed to turn a
negotiate URI into one fully ready, then eventually stopped, connection
*generation*.

### 1. `SignalRSocketConnectionFactory`

The production `ISocketConnectionFactory`. It is constructed once (by
`GeoSocket`'s owner) with an `ISocketModeNegotiator`, the readiness/keep-alive/
server timeouts, a logger, and an optional `CreateSignalRClientConnection`
override for tests. `Create(negotiateUri, handlers)` just news up a
`SignalRSocketConnection` with those captured settings -- one instance per
generation, never reused across generations.

### 2. `SignalRSocketConnection` -- fields and construction

Holds the negotiate URI, the `SocketConnectionHandlers` bundle it was handed,
the negotiator, the client-connection factory delegate, the three timeouts,
and a logger. Three pieces of internal state matter most:

- `_lifetimeSource` -- a `CancellationTokenSource` scoped to this one
  generation; cancelling it tears down everything this generation started.
- `_readySource` -- a `TaskCompletionSource` that the "is this generation
  ready yet" gate (`TokenLifetime`/`StartAsync`) waits on.
- `_started` / `_stopped` / `_disposed` / `_readySettled` / `_closedReported`
  -- `Interlocked`-guarded flags so every transition (start-once, stop-once,
  ready-once, closed-reported-once) is race-safe even if multiple callers
  race to stop/dispose/close concurrently.

### 3. `StartAsync` -- negotiate, connect, wait for ready

This is the heart of the class, run once per generation:

1. Guards against double-start with `Interlocked.Exchange(ref _started, 1)`.
2. Links the caller's `cancellationToken` with `_lifetimeSource.Token` so
   either an external cancellation or an internal stop aborts startup.
3. Calls `_negotiator.NegotiateAsync(_negotiateUri, ...)` to get a
   `SocketModeNegotiateResponse` (SignalR URL + access token + `ExpiresIn`).
   `TokenLifetime` is set from `ExpiresIn` here -- this is what `GeoSocket`
   later reads to schedule proactive token-rotation refreshes.
4. Builds the actual client via `_createSignalRClientConnection(...)` --
   i.e. calls into `SignalRClientConnection.Create` (or a test double) with
   the negotiated URL/token and the configured keep-alive/server timeouts.
5. Publishes the connection into `_connection` under a lock
   (`TryPublishConnection`) -- but only if this generation hasn't already
   been stopped/disposed out from under it; otherwise it disposes the
   just-built connection immediately rather than leaking it.
6. Wires the three handlers onto the new connection:
   - `OnActivity` -- **waits on `_readySource.Task` first**, then forwards to
     `_handlers.OnActivity`. This means an activity frame that somehow
     arrives before `SocketReady` is held until readiness, not dropped or
     dispatched early.
   - `OnReady` -- wired to the private `HandleReady`.
   - `OnClosed` -- wired to the private `HandleClosed`.
7. Calls `connection.StartAsync(...)`, then awaits `_readySource.Task` with
   `WaitAsync(_readinessTimeout, ...)`. A timeout here is rethrown as a
   `TimeoutException` naming `_readinessTimeout` explicitly -- this is what
   ultimately surfaces as a `GeoSocket`/`SocketModeTransport` startup failure
   if a generation never reports ready in time.
8. Any exception anywhere in this sequence routes through a single `catch`
   that calls `StopAfterFailedStartAsync()` (best-effort stop + dispose,
   with failures only logged, never masking the original exception) before
   rethrowing.

### 4. `HandleReady` and `HandleClosed`

- `HandleReady` first checks `_lifetimeSource.IsCancellationRequested` and
  uses `Interlocked.CompareExchange(ref _readySettled, ...)` so it only ever
  fires once. It resolves `_readySource` **before** invoking
  `_handlers.OnReady(frame)`, and wraps that callback in a try/catch that
  only logs -- a throwing observer must never leave the readiness gate
  unsettled.
- `HandleClosed` checks whether the closure was planned (`_stopped != 0`).
  If it was *not* planned and readiness was never settled, it fails
  `_readySource` with the closure error (or a synthesized `IOException`) so
  a caller awaiting startup sees the real failure instead of hanging. It
  then reports to `_handlers.OnClosed(error, planned)` exactly once, guarded
  by `_closedReported`.

### 5. `StopAsync` / `DisposeAsync`

`StopAsync` is idempotent and memoized (`_stopTask ??= StopCoreAsync()`
under `_stopLock`), so concurrent callers all await the same underlying
stop. `StopCoreAsync` cancels `_lifetimeSource` first, then stops the
published `ISignalRClientConnection` if one exists. `DisposeAsync` calls
`StopAsync` first, then disposes the underlying connection, then disposes
`_lifetimeSource` -- all in `finally` blocks so a failure at one stage still
lets later cleanup run.

**Takeaway:** `SignalRSocketConnection` is the layer that turns "negotiate +
build a client connection" into a single well-behaved, race-safe,
once-only-everything `ISocketConnection` generation -- readiness gating,
token-lifetime reporting, and planned-vs-unplanned closure classification
all live here. This is also the class flagged earlier as a candidate for
being folded directly into `GeoSocket` (removing one layer), since today
`GeoSocket` is the only caller of `ISocketConnectionFactory`.

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
The production implementation is `SignalRSocketConnectionFactory` (see
above), which negotiates via `SocketModeNegotiator` and then builds a
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
(`SignalRSocketConnection`, covered above) be swapped or mocked
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
SignalR exists at all. All three concrete classes described in this doc --
`SignalRClientConnection`, `SignalRSocketConnectionFactory`/
`SignalRSocketConnection`, and the interfaces in `SocketModeConnection.cs`
-- are already on `main`.
