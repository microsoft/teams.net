# GeoSocket Walkthrough

> This is a supplementary, implementation-level walkthrough of `GeoSocket`
> for engineers working on the Socket Mode PR stack. See
> [SocketMode-Design.md](./SocketMode-Design.md) for the user-facing design
> and architecture overview, and
> [SocketModeTransport-Walkthrough.md](./SocketModeTransport-Walkthrough.md)
> for the coordinator that owns one `GeoSocket` per geo. The class described
> here lives on the `teddyam-socket-mode-app-integration` branch
> ([#686](https://github.com/microsoft/teams.net/pull/686)) and is not yet on
> `main`.

`GeoSocket` manages **one geo's** connection lifecycle: establishing the
initial connection, supervising it in the background, reconnecting after
unexpected closures, and rotating tokens make-before-break. Here is how it
works, step by step.

## 1. Construction

`GeoSocket(owner, geo, negotiateUri, connectionFactory, logger, timeProvider)`
just stores its dependencies. `_owner` is the `IGeoSocketOwner` (in practice,
`SocketModeTransport`) that supplies timeouts, backoff policy, and the
dispatch callback. No connection is opened yet.

## 2. `StartAsync()` -- initial connection

- Guards against double-start via `Interlocked.Exchange(ref _started, 1)`.
- Calls `ConnectInitialAsync()`, which retries `ConnectAsync()` in a loop
  until either it succeeds, the `StartupTimeout` budget runs out (throws
  `TimeoutException`), or a non-retryable negotiate failure occurs (HTTP
  401/403 -- thrown immediately, no retry).
- Once a `Generation` is ready, spawns the background `SuperviseAsync(initial)`
  loop and returns. This is what `SocketModeTransport.StartAsync` awaits,
  per geo.

## 3. `ConnectAsync()` -- one connection attempt

- Increments `_generation` and creates a new `Generation` record (an id plus
  a `TaskCompletionSource<CloseReason>` called `Closed`).
- Creates the actual `ISocketConnection` via `_connectionFactory.Create(...)`,
  wiring three callbacks: dispatch, ready, and closed.
- Adds the connection to `_owned` -- the set of every connection this geo
  currently owns, including ones being retired.
- Calls `connection.StartAsync()`. The underlying SignalR connection's
  `StartAsync` completes once `SocketReady` arrives, but `TryPromote` (next
  step) confirms the connection actually became the active generation before
  `ConnectAsync` reports success.
- On success, calls `ScheduleRefresh(generation)` to arm the token-rotation
  timer.

## 4. `TryPromote()` -- generation handoff

This is the core of make-before-break rotation: when a new generation is
ready, `TryPromote` atomically marks the *previous* active generation as
retiring (added to the `_retiring` set) and swaps `_active` to the new one.
At any instant there is at most one active generation, plus zero or more
retiring generations still allowed to dispatch.

## 5. `DispatchAsync(generation, envelope)`

Before forwarding to `_owner.DispatchAsync`, this checks that the generation
is still `_active` **or** present in `_retiring`. A fully-replaced,
already-closed generation's stray in-flight messages are dropped (returns
`null`), so a zombie connection cannot process activities after handoff.

## 6. `ScheduleRefresh()` + `RequestRotation()` -- proactive token rotation

If the connection reports a `TokenLifetime`, a timer is armed for
`lifetime - TokenRefreshMargin`. When it fires, `RequestRotation` does not
tear down the connection itself -- it simply completes the generation's
`Closed` task-completion-source with `Planned: true`, signaling the
supervisor loop to react.

## 7. `SuperviseAsync()` -- the main loop

Waits on `current.Closed.Task`. Two paths:

- **Planned** (rotation): calls `ReconnectAsync(null, delayFirstAttempt: false)`
  to connect a *new* generation immediately (no delay). Only if that succeeds
  does it call `StartRetirement(current)` to retire the old one -- new
  connection first, old one after, i.e. make-before-break.
- **Unplanned** (real disconnect): releases the dead connection immediately,
  then calls `ReconnectAsync(reason.Error, delayFirstAttempt: true)`, which
  retries with backoff.

If `ReconnectAsync` gives up after a non-retryable error, the loop exits --
`StopAfterNonRetryable` has already stopped the geo permanently and reported
it via `OnGeoDisconnected`.

## 8. `StartRetirement()` / `RetireAsync()` -- the handoff window

Runs as a fire-and-forget task (tracked in `_retirements` so `StopAsync` can
wait for it): sleeps for `HandoffWindow`, then removes the generation from
`_retiring` and calls `ReleaseAsync` to stop and dispose it. During that
sleep, the old connection can still dispatch (per step 5's check), giving
in-flight activities time to finish before it is torn down.

## 9. `HandleClosed()` -- bookkeeping on any close

Marks the generation closed and removes it from `_retiring`. Only if it was
the `_active` generation, the close was unplanned, and the geo is not
stopping does it flag `_disconnected` and call `_owner.OnGeoDisconnected`.
It always completes the generation's `Closed` task-completion-source so
`SuperviseAsync` wakes up.

## 10. `StopAsync()` / `DisposeAsync()`

Idempotent via `_stopTask`. Cancels `_stopSource`, clears `_active` and
`_retiring`, stops and disposes every connection in `_owned` in parallel,
then waits for any in-flight retirement tasks and the supervisor loop to
finish.

## Summary

`GeoSocket` treats each connection attempt as a numbered "generation," uses
an `_active` / `_retiring` split to let two generations briefly coexist
during rotation, and drives all state transitions through each generation's
`Closed` signal rather than direct cancellation. That design is what lets
make-before-break handoff and reconnect-on-failure converge into a single
supervisor loop.
