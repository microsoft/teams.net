# SocketModeTransport Walkthrough

> This is a supplementary, implementation-level walkthrough of `SocketModeTransport`
> for engineers working on the Socket Mode PR stack. See
> [SocketMode-Design.md](./SocketMode-Design.md) for the user-facing design and
> architecture overview. The class described here lives on the
> `teddyam-socket-mode-app-integration` branch
> ([#686](https://github.com/microsoft/teams.net/pull/686)) and is not yet on
> `main`. See also
> [GeoSocket-Walkthrough.md](./GeoSocket-Walkthrough.md) for the per-geo
> connection supervisor that `SocketModeTransport` owns.

`SocketModeTransport` is the coordinator that owns one `GeoSocket` per
configured geo, supplies them with retry/backoff policy (as their
`IGeoSocketOwner`), and turns raw inbound envelopes into dispatch calls and
reply frames. Here is how it works, step by step.

## 1. Construction

`SocketModeTransport(options, connectionFactory, dispatch, logger, ...)` does
validation and setup only -- no network activity yet:

- Validates `NegotiateBaseUri` is non-null and `StartupTimeout >= 0`.
- Calls `ResolveGeos()` to turn each geo string (`"amer"`, `"emea"`, `"apac"`)
  into a full negotiate `Uri` (base URL + `/{geo}/negotiate`), rejecting
  duplicate or empty geo entries.
- Stores the `dispatch` delegate (how an activity reaches the bot pipeline)
  and the optional `onError` hook.
- `Status` starts as `Idle`.

## 2. `StartAsync()` -- bringing geos up

- Guards against double-start (`_lifecycle != Idle` throws).
- Creates one `GeoSocket` per resolved geo, passing `this` as the
  `IGeoSocketOwner` -- this is how each `GeoSocket` gets its retry policy,
  timeouts, and dispatch callback.
- Marks every geo `Connecting` in `_geoStatuses`.
- Runs `geoSocket.StartAsync()` for **all geos concurrently** via
  `Task.WhenAll`.
- **All-or-nothing startup**: if any geo fails, a shared `startSource` is
  cancelled so the other geos abandon their attempt, the *first* failure is
  captured via `Interlocked.CompareExchange`, `StopAsync()` tears everything
  down, and that first failure is rethrown. This is why a Socket Mode startup
  failure surfaces directly from `host.Run()`.

## 3. Steady state -- acting as `IGeoSocketOwner`

Once started, `SocketModeTransport` mostly answers callbacks from the running
`GeoSocket`s:

- `GetBackoffDelay(attempt)` -- indexes into the configured `ReconnectDelays`
  schedule (clamped to the last entry) or computes capped exponential backoff
  with jitter.
- `GetRetryAfter(error)` -- pulls a server-provided retry hint off a
  `SocketModeNegotiateException`, if present.
- `OnGeoReady` / `OnGeoDisconnected` / `OnGeoReconnected` -- update
  `_geoStatuses[geo]` under a lock.
- `DispatchAsync(geo, envelope)` -- forwards to `HandleEnvelopeAsync`.

## 4. `HandleEnvelopeAsync()` -- per-activity processing

For each inbound envelope:

1. Rejects unsupported protocol versions with a `400` reply.
2. Extracts the `CoreActivity` payload; drops the envelope silently if there
   is none.
3. Determines whether it is an `invoke` activity (needs a reply) versus
   fire-and-forget.
4. Calls `_dispatch(activity)` -- this is where
   `TeamsBotApplication.ProcessWithInvokeResponseAsync` plugs in.
5. On success: builds an invoke reply or a plain acknowledgement frame with
   the result status.
6. On exception: logs it (unless already logged as a `BotHandlerException`),
   invokes the optional `_onError` hook, and returns a `500` reply/ack instead
   of throwing -- a single bad activity cannot kill the geo connection.

## 5. Aggregate `Status` property

Computed on read, not stored directly:

- `Ready` only if every geo is `Ready`.
- `Disconnected` if any geo is down.
- `Connecting` otherwise.

This is what a health check or the hosted service polls to decide whether the
transport, as a whole, is serving traffic.

## 6. `StopAsync()` / `DisposeAsync()`

Idempotent (memoized via `_stopTask`): flips `_lifecycle` to `Stopped`, marks
every geo `Stopped`, and disposes all `GeoSocket` instances in parallel,
logging (rather than throwing) individual cleanup failures.

## Summary

`GeoSocket` handles the low-level "keep one socket alive" mechanics for a
single geo. `SocketModeTransport` is the coordinator: it fans `StartAsync`
out to all geos with all-or-nothing semantics, supplies policy via
`IGeoSocketOwner`, and turns raw envelopes into dispatch calls and reply
frames.
