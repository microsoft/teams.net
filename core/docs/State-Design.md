# State Management Design Document

## Overview

`TurnState` provides persistence of data **across turns** within a conversation and **across conversations** for a single user. Developers store small conversation state or user preferences without manually deriving storage keys or managing read/write lifecycles. (It is **not** a place for large or unbounded data, nor for accumulating collections such as message history — last-write-wins saves can silently drop concurrent appends; see [What TurnState is NOT](#what-turnstate-is-not).)

State **loads automatically at the start of each turn** and **saves automatically when the turn handler completes successfully**. The feature lives in **`Microsoft.Teams.Apps`** (namespace `Microsoft.Teams.Apps.State`): `TeamsBotApplication.OnActivity` loads state through a `TurnStateStore`, passes the `TurnState` into the per-turn `Context`, and saves on success — mirroring how `OAuthFlow` (see [OAuthFlow-Design.md](sso/OAuthFlow-Design.md)) is an Apps-layer abstraction built on Core primitives. **Core is not modified.**

Key design choices:

- **Backed by `IDistributedCache`.** State persists through the standard .NET [`IDistributedCache`](https://learn.microsoft.com/aspnet/core/performance/caching/distributed) abstraction rather than a bespoke storage interface. This is the same abstraction ASP.NET Core session state uses, so **every existing cache backend works for free** — `AddDistributedMemoryCache` (in-process dev), `AddStackExchangeRedisCache` / Garnet (multi-instance), `AddDistributedSqlServerCache`, NCache, etc. The SDK ships and maintains no storage providers of its own. `UseState` defaults to an in-process cache, so a backend is only registered explicitly to override it (e.g. Redis for multi-instance).
- **Two scopes.** `Conversation` (per conversation) and `User` (per user). Both persist. There is no transient/temp scope — pass per-turn data through the existing `Context` rather than state.
- **Last-write-wins.** Concurrency is last-write-wins. `IDistributedCache` has no compare-and-swap, so there is no optimistic-concurrency / ETag path.
- **System.Text.Json, reusing the canonical context.** A scope is an open-typed bag (`Dictionary<string, object?>` of arbitrary user POCOs), so serialization is reflection-based. `StateSerializer` reuses the existing `TeamsActivityJsonContext` for source-generated metadata on common primitives/`JsonElement` values, combined with a reflection resolver for user types. Documents are bare camelCase UTF-8 JSON.

If `UseState` is not called, `context.State` is `null` and the bot behaves exactly as before. State is completely opt-in.

## Motivation

Without `TurnState`, a bot author who needs to remember anything across turns must derive a stable key from `Activity.ChannelId` / `Conversation.Id` / `From.Id` by hand, pick and wire a backend, and read/mutate/write the document in every handler (remembering *not* to write on failure). `TurnState` reduces this to:

```csharp
var count = ctx.State?.Conversation.Get<int>("count") ?? 0;
ctx.State?.Conversation.Set("count", count + 1);
```

…with key derivation, change tracking, atomic save, and JSON handling owned by the framework, and the backend supplied by any `IDistributedCache` the app already has.

## Architecture

```
TeamsBotApplication (Apps)
├── OnActivity ───────────────────────────► loads TurnState (TurnStateStore) at turn start,
│                                            passes it into Context, saves on success
├── Router
│   └── ... routes dispatch Context<TActivity> (each carries the same TurnState) ...
└── Context<TActivity>
    └── State  ──► TurnState (passed into the constructor for this turn)
                   ├── Conversation : StateScope   (persisted)
                   └── User         : StateScope   (persisted)

Backend: IDistributedCache (resolved from DI)
├── AddDistributedMemoryCache       in-process, dev / single-instance
├── AddStackExchangeRedisCache      multi-instance (Redis / Garnet)
├── AddDistributedSqlServerCache    SQL Server
└── any other IDistributedCache     NCache, Azure, custom …
```

### Where state lives during a turn

State load/save runs in `TeamsBotApplication.OnActivity`, around routing:

```
OnActivity(activity, ct)
    │
    ├─ 1. teamsActivity = FromActivity(activity)
    ├─ 2. turnState = await store.LoadAsync(teamsActivity)   // derive keys, GetAsync both scopes
    ├─ 3. defaultContext = new Context(this, teamsActivity, turnState)   // state passed in explicitly
    │
    ├─ 4. try: dispatch routes (each per-route Context is built with the same turnState)
    │
    ├─ 5. await store.SaveAsync(turnState)   // changed scopes only — reached only if dispatch didn't throw
    └─ 6. finally: turnState.Complete()      // seal: later scope access throws
```

The `TurnState` is **threaded explicitly** through the `Context` constructor — there is no ambient/`AsyncLocal`. [`Route.InvokeRoute`](../src/Microsoft.Teams.Apps/Routing/Route.cs) builds a fresh per-route `Context` and forwards `ctx.State`, so every route in a multi-match turn shares the one `TurnState`.

If an exception propagates out of dispatch, step 5 is skipped — **no writes occur**. This atomic-save guarantee comes from the `try`/`finally` in `OnActivity`.

## Scopes

| Scope | Lifetime | Persists? | Storage key (logical) | Use case |
|---|---|---|---|---|
| **Conversation** | Entire conversation (all turns, all participants) | ✅ Yes | `{channelId}/conversations/{conversationId}` | Dialog state, turn counter, shared conversation data |
| **User** | Across all conversations with this user | ✅ Yes | `{channelId}/users/{fromId}` | User preferences, display name, settings |

Keys are derived from the inbound `CoreActivity` (`channelId` ← `activity.ChannelId`, `conversationId` ← `activity.Conversation.Id`, `fromId` ← `activity.From.Id`). Including `channelId` in both keys prevents state from leaking across channels/tenants. Key derivation is a single seam (`TurnState.DeriveKeys`) so the scheme can evolve without touching handlers. When a key part is missing (e.g. an activity with no `From`), that scope is hydrated empty and never written.

### Conversation scope

```csharp
bot.OnMessage(async (ctx, ct) =>
{
    var count = ctx.State?.Conversation.Get<int>("turnCount") ?? 0;
    count++;
    ctx.State?.Conversation.Set("turnCount", count);
    await ctx.SendAsync($"Turn #{count}", ct);
});
```

### User scope

```csharp
bot.OnMessage(async (ctx, ct) =>
{
    var name = ctx.State?.User.Get<string>("displayName");
    if (name is null)
    {
        name = ctx.Activity.From?.Name ?? "User";
        ctx.State?.User.Set("displayName", name);
    }
    await ctx.SendAsync($"Hello, {name}!", ct);
});
```

## API Surface

### Registration

Opt in with `UseState()`. It defaults to an in-process cache, so nothing else is required; register a distributed backend only to override it.

```csharp
// Defaults to an in-process IDistributedCache:
builder.Services.AddTeamsBotApplication(options => options.UseState());

// For multi-instance, register a distributed cache (takes precedence over the default):
builder.Services.AddStackExchangeRedisCache(o => o.Configuration = "localhost:6379");
builder.Services.AddTeamsBotApplication(options => options.UseState());
```

`UseState` on `TeamsBotApplicationOptions` (and the equivalent `AppBuilder.UseState`):

```csharp
public sealed class TeamsBotApplicationOptions
{
    /// Enable state backed by the application's IDistributedCache (resolved from DI; defaults to
    /// in-process). entryOptions applies a TTL (sliding/absolute) to every written document.
    public TeamsBotApplicationOptions UseState(DistributedCacheEntryOptions? entryOptions = null);
}
```

`UseState` sets a flag (and optional TTL) on the options. `AddTeamsBotApplication` then registers a `TurnStateStore` (over `AddDistributedMemoryCache`'s default cache, or any explicitly registered `IDistributedCache`) as a DI singleton, which is **injected into the `TeamsBotApplication` constructor**. Options stays pure configuration — it does not carry the cache or the store.

### `TurnState` and scopes

```csharp
namespace Microsoft.Teams.Apps.State;

public sealed class TurnState
{
    /// Per-conversation persisted scope.
    public StateScope Conversation { get; }

    /// Per-user persisted scope.
    public StateScope User { get; }

    /// True once the turn has completed and state has been saved. After this, every scope
    /// read/write throws (see "Lifetime and after-turn access").
    public bool IsCompleted { get; }

    /// Path access: "conversation.count" or "user.name". A bare key (no scope prefix) throws.
    public T? GetValue<T>(string path);
    public void SetValue<T>(string path, T value);
}

public sealed class StateScope
{
    // All members throw InvalidOperationException if the owning turn has completed.
    public T? Get<T>(string key);
    public void Set<T>(string key, T value);
    public bool Remove(string key);
    public bool ContainsKey(string key);
    public void Clear();
}
```

`Get<T>` reuses the same `JsonElement` round-trip logic as `ExtendedPropertiesDictionary.Get<T>` (caching the deserialized value back into the dictionary), so deserialization happens once per key per turn.

### `Context<TActivity>.State`

```csharp
public class Context<TActivity> where TActivity : TeamsActivity
{
    /// The state for the current turn, or null if state is not enabled.
    public TurnState? State { get; }
}
```

`State` is set from the `TurnState` passed into the `Context` constructor by `OnActivity` (and forwarded to each per-route `Context`), so it stays consistent whether read from a route handler or an OAuth callback.

## Internal Flow

### Turn load/save sequence

```
OnActivity(activity, ct)
    │
    ├─ 1. (convKey, userKey) = TurnState.DeriveKeys(activity)
    │
    ├─ 2. store.LoadAsync: concurrent cache.GetAsync per key → bytes → StateSerializer.Deserialize
    │     (IDistributedCache has no batch read; a turn reads at most two keys)
    │
    ├─ 3. turnState = new TurnState(conversation, user, convKey, userKey)
    │     baseline[scope] = StateSerializer.Serialize(scope)   // load-time byte snapshot
    │     Context(this, teamsActivity, turnState)              // state passed in explicitly
    │
    ├─ 4. dispatch routes (within try)
    │
    ├─ 5. // only reached when dispatch did NOT throw — store.SaveAsync:
    │     foreach persisted scope that changed (re-serialize, compare to baseline):
    │         scope.IsEmpty ? cache.RemoveAsync(key)               // emptied this turn → delete
    │                       : cache.SetAsync(key, bytes, entryOptions)
    │
    └─ 6. finally: turnState.Complete()  (seal: later scope access throws)
```

**Write-only-if-changed** — each persisted scope is serialized to a UTF-8 byte baseline at load and re-serialized at save; only scopes whose bytes differ are written. This catches in-place mutation of a fetched reference type (`Get<List<string>>(...).Add(...)`) that a dirty flag would miss, and avoids a round-trip on unchanged turns. Cost is one extra serialization per persisted scope per turn — the same trade Bot Framework, Teams AI, and the Agents SDK all make.

**Delete-on-empty** — a changed scope that ends the turn empty (e.g. `scope.Clear()`) is routed to `RemoveAsync` instead of writing an empty document, so a cleared scope leaves no orphan entry. Because the delete only fires when the scope *changed* to empty, an always-empty scope never issues a spurious delete.

### Serialization

A scope is an open-typed `Dictionary<string, object?>`, so serialization is reflection-based. Rather than stand up a parallel state-specific source-gen context, `StateSerializer` reuses the canonical `TeamsActivityJsonContext`:

```csharp
internal static readonly JsonSerializerOptions Options = new()
{
    TypeInfoResolver = JsonTypeInfoResolver.Combine(TeamsActivityJsonContext.Default, new DefaultJsonTypeInfoResolver()),
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

internal static byte[] Serialize(IDictionary<string, object?> values) => JsonSerializer.SerializeToUtf8Bytes(values, Options);
internal static Dictionary<string, object?> Deserialize(ReadOnlySpan<byte> utf8Json) => JsonSerializer.Deserialize<...>(utf8Json, Options) ?? [];
```

Serialization is byte-native (no string intermediary): the same bytes are used for the change-detection baseline and written straight to the cache. Values written via `Set<T>` are held boxed in-memory and serialized lazily on save; values read via `Get<T>` deserialize the stored `JsonElement` on first access and cache the typed result.

### Lifetime and after-turn access

`TurnState` is **scoped to a single turn**: loaded at the start and **sealed** at the end (`turnState.Complete()` in the `OnActivity` `finally`). Accessing it after the turn — almost always by capturing `ctx.State` in fire-and-forget background work — is a misuse, because the atomic save has already run and will not run again.

The captured `ctx.State` is a live reference, so a naive design would let background code read a **stale** copy and write changes that **silently never persist**. To make that loud, every scope read/write checks `IsCompleted` and throws:

```csharp
// inside StateScope.Get/Set/Remove/...
if (_completed)
    throw new InvalidOperationException(
        "TurnState was accessed after the turn completed. State is per-turn and is saved once " +
        "when the handler returns. Read the values you need during the turn and pass them into " +
        "any background work, e.g. `var name = ctx.State.User.Get<string>(\"name\");`.");
```

This fires *through* the idiomatic `ctx.State?.` access (the scope method throws, not the null-conditional), so the failure cannot be swallowed. Correct pattern: read values out **during** the turn and pass those into the background work.

## Path Syntax (Advanced)

Instead of the scope properties, a scope-qualified path string can be used. A bare key (no scope prefix) throws `ArgumentException`.

| Path | Equivalent |
|---|---|
| `"conversation.count"` | `state.Conversation.Get<int>("count")` |
| `"user.name"` | `state.User.Get<string>("name")` |
| `"foo"` | ❌ throws — must be scope-qualified |

```csharp
ctx.State?.SetValue("conversation.count", 5);   // same as Conversation.Set("count", 5)
```

## Atomic Semantics

State is **saved only when the turn handler completes successfully**. If the handler throws, **state mutations are discarded** — nothing is written. A failed turn never leaves persisted state in a partially-updated condition.

```csharp
bot.OnMessage(async (ctx, ct) =>
{
    ctx.State?.Conversation.Set("count", 42);

    if (ctx.Activity.Text == "crash")
        throw new InvalidOperationException("Simulated error");   // count is NOT persisted

    await ctx.SendAsync("OK", ct);                                // success → count persists
});
```

This is implemented by the middleware writing only after `await next(ct)` returns normally.

## Backends

Any `IDistributedCache` works. The notable trade-off is the document format:

| Backend | Registration | Notes |
|---|---|---|
| **In-memory** | `AddDistributedMemoryCache()` | In-process, lost on restart. Dev / single-instance. |
| **Redis / Garnet** | `AddStackExchangeRedisCache(...)` | Multi-instance. ⚠️ The built-in `RedisCache` wraps values in its own Redis hash (with expiry metadata), so documents are **not** directly readable by the Node/Python SDKs. |
| **SQL Server** | `AddDistributedSqlServerCache(...)` | Multi-instance, durable. |
| **Other** | NCache, Azure, custom | Any `IDistributedCache` implementation. |

**Cross-runtime documents:** the value written is bare camelCase UTF-8 JSON with no .NET-specific markers, so a backend that stores the value *verbatim* keeps it interoperable across .NET / Node / Python bots. The built-in `RedisCache` does not store verbatim (see above) — use a backend that does if cross-SDK document interop is required.

**Eviction:** `IDistributedCache` is a *cache* contract; a backend may evict entries (`MemoryDistributedCache` evicts under memory pressure). Use a durable backend, and avoid aggressive eviction policies on state keys, when state must not be lost.

**Expiration:** pass `DistributedCacheEntryOptions` to `UseState` to apply a sliding/absolute TTL to every written document.

## File Placement

| File | Location |
|---|---|
| `TurnState.cs` | `Microsoft.Teams.Apps/State/TurnState.cs` |
| `StateScope.cs` | `Microsoft.Teams.Apps/State/StateScope.cs` |
| `TurnStateStore.cs` | `Microsoft.Teams.Apps/State/TurnStateStore.cs` |
| `StateSerializer.cs` | `Microsoft.Teams.Apps/State/StateSerializer.cs` |

`UseState` lives on `TeamsBotApplicationOptions` and `AppBuilder`; the `TurnStateStore` is created in the `TeamsBotApplication` constructor and driven from `OnActivity`.

## Changes to Core / Apps

| File | Change |
|---|---|
| `Context.cs` | `Context(..., TurnState? turnState)` constructor param; `public TurnState? State { get; }` set from it. |
| `Routing/Route.cs` | Per-route `Context` construction forwards `ctx.State` to the typed context. |
| `TeamsBotApplicationOptions.cs` | `UseState(...)` + internal `StateEnabled` / `StateEntryOptions` (pure config — no cache/store reference). |
| `TeamsBotApplication.HostingExtensions.cs` | When state is enabled, register a default `AddDistributedMemoryCache` + a `TurnStateStore` DI singleton over the resolved `IDistributedCache`. |
| `TeamsBotApplication.cs` | `TurnStateStore?` injected via the constructor (null = state off); load/save around routing in `OnActivity`. |
| `AppBuilder.cs` | `UseState(...)` delegating to `Options.UseState`. |

## Edge Cases & Constraints

| Scenario | Behavior |
|---|---|
| State not enabled (`UseState` not called) | `context.State` is `null`. Bot behaves exactly as before (fully opt-in). |
| `UseState()` with no cache registered | Defaults to an in-process `IDistributedCache` (`AddDistributedMemoryCache`). Register a distributed backend to override. |
| Activity has no `From` | User scope key is null → user scope is empty and never written. Conversation scope still works. |
| Activity has no `Conversation` | Conversation scope is empty and never written. |
| Handler throws | No writes occur (atomic). |
| Unchanged turn (no mutation) | No backend write — each scope's save-time bytes match its load-time baseline. |
| Concurrent turns on the same key | Last-write-wins (`IDistributedCache` has no compare-and-swap). A read-modify-write of a growing collection (e.g. appending messages) can drop the earlier write — see [What TurnState is NOT](#what-turnstate-is-not). Keep state to small, last-writer-safe values. |
| Bare path (`SetValue("foo", …)`) | Throws `ArgumentException` — paths must be scope-qualified. |
| Accessing state **after the turn** | Scope reads/writes throw a descriptive `InvalidOperationException` (sealed on `IsCompleted`). Capture values during the turn instead. |
| Backend evicts an entry | State for that key is lost on next read (cache semantics). Use a durable backend / no eviction for state keys. |
| Sensitive values | Stored as plain JSON. Encrypt before `Set` if required. |

## What TurnState is NOT

- **Not a database** — optimized for small per-conversation / per-user key-value documents.
- **Not for large blobs** — serializes to JSON; store small objects. Large files belong in Blob storage.
- **Not a message log / accumulating collection** — do **not** append incoming messages (or grow any list) in state. Saves are **last-write-wins** with no compare-and-swap, so two concurrent turns in the same conversation each load the list, append, and write back — and the later write clobbers the earlier one, silently dropping messages (drift). Read-modify-write of a growing collection is exactly the pattern this concurrency model can't protect. Persist message history in an append-only store (a database/Blob) and keep only small, last-writer-safe values (a counter, a flag, the current dialog step) in state.
- **Not a distributed lock** — last-write-wins; do not use for coordination.
- **Not transient scratch space** — there is no temp scope; pass per-turn data through `Context`.

## See Also

- [Architecture.md](Architecture.md) — how the three layers and the middleware pipeline fit together.
- [OAuthFlow-Design.md](sso/OAuthFlow-Design.md) — the layering / opt-in pattern this design mirrors.
