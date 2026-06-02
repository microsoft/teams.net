# State Management Design Document

## Overview

`TurnState` provides persistence of data **across turns** within a conversation, **across conversations** for a single user, and **within a single turn** (scratch space). Developers store small conversation state, user preferences, or transient per-turn data without manually deriving storage keys or managing read/write lifecycles. (It is **not** a place for large or unbounded data such as full message history — see [Requirement: keep each scope small](#requirement-keep-each-scope-small).)

State **loads automatically at the start of each turn** and **saves automatically when the turn handler completes successfully**. The entire feature lives in the **`Microsoft.Teams.Apps`** layer (namespace `Microsoft.Teams.Apps.State`): a `StateMiddleware` plugs into the turn pipeline and the result is surfaced as `context.State` — mirroring how `OAuthFlow` (see [OAuthFlow-Design.md](sso/OAuthFlow-Design.md)) is an Apps-layer abstraction built on Core primitives. **Core is not modified.**

The design honors the existing architecture (see [Architecture.md](Architecture.md)):

- **Apps owns the feature** — `IStorage`, `StoreItem`, `MemoryStorage`, `TurnState`, the scopes, and `StateMiddleware` all live in `Microsoft.Teams.Apps/State/`. The only Core touchpoints are implementing `ITurnMiddleware` and converting the inbound activity to `TeamsActivity`.
- **Middleware Pipeline pattern** — `StateMiddleware` implements Core's `ITurnMiddleware` (explicitly) and exposes a Teams-typed `OnTurnAsync(TeamsBotApplication, TeamsActivity, …)`. It wraps `OnActivity` in the existing `TurnMiddleware` pipeline (the `Load → next() → Save` envelope the pipeline already supports).
- **Context pattern** — `Context<TActivity>` exposes `State`, keeping the same opt-in, null-when-unused ergonomics as `OAuthRegistry`.
- **System.Text.Json, reusing the canonical context** — a state scope is an open-typed bag (`Dictionary<string, object?>` of arbitrary user POCOs), so it cannot be a closed-world, fully source-generated serializer like the activity pipeline. `StateSerializer` reuses the existing `TeamsActivityJsonContext` for source-generated metadata on the primitives and `JsonElement` values that commonly appear, combined with a reflection resolver for user types. No parallel state-specific JSON context.
- **Opt-in distribution** — `RedisStorage` ships in a separate `Microsoft.Teams.State.Redis` package and is never pulled into the core install.

If state middleware is not registered, `context.State` is `null` and the bot behaves exactly as before. State is completely opt-in.

## Motivation

Today a bot author who needs to remember anything across turns must:

1. Derive a stable storage key from `Activity.ChannelId`, `Activity.Conversation.Id`, and `Activity.From.Id` by hand.
2. Pick and wire a storage backend.
3. Read the document at the top of every handler, mutate it, and write it back — remembering to *not* write it back on failure.
4. Serialize/deserialize values, handling the `JsonElement` round-trip that `ExtendedPropertiesDictionary.Get<T>` already has to deal with.

Every handler re-implements the same load/mutate/save boilerplate, and the key-derivation logic is easy to get subtly wrong (e.g. forgetting `ChannelId`, leaking user state across tenants). `TurnState` reduces this to:

```csharp
var count = ctx.State?.Conversation.Get<int>("count") ?? 0;
ctx.State?.Conversation.Set("count", count + 1);
```

…with key derivation, change tracking, atomic save, and JSON handling owned by the framework.

## Architecture

```
TeamsBotApplication (Apps)
├── MiddleWare (TurnMiddleware, from Core)
│   ├── ... existing middleware ...
│   └── StateMiddleware ────────────────► loads TurnState at turn start,
│                                          saves on success, discards on throw
├── Router
│   └── ... routes dispatch Context<TActivity> ...
└── Context<TActivity>
    └── State  ──► TurnState (ambient for the current turn)
                   ├── Conversation : StateScope   (persisted)
                   ├── User         : StateScope   (persisted)
                   └── Temp         : StateScope   (never persisted)

IStorage (Apps — Microsoft.Teams.Apps.State)
├── MemoryStorage   (Apps)                in-process, ConcurrentDictionary
├── FileStorage     (Apps, future)        JSON files, single-instance only
└── RedisStorage    (Microsoft.Teams.State.Redis, opt-in package → references Apps)
```

### Where state lives during a turn

The state middleware runs inside the existing pipeline. `BotApplication.ProcessAsync` already calls:

```csharp
await MiddleWare.RunPipelineAsync(this, activity, this.OnActivity, 0, token);
```

`StateMiddleware.OnTurnAsync` wraps the remainder of the pipeline (including `OnActivity`, where `Context` is constructed and routes dispatch):

```
StateMiddleware.OnTurnAsync(botApp, activity, next, ct)
    │
    ├─ 1. Derive keys from activity (channelId / conversation.id / from.id)
    ├─ 2. storage.ReadAsync([convKey, userKey])  → hydrate Conversation + User scopes
    ├─ 3. Publish TurnState into the ambient accessor (AsyncLocal)
    │
    ├─ 4. await next(ct)        ◄── OnActivity → Context.State sees the ambient TurnState
    │
    ├─ 5. (next returned without throwing)
    │     storage.WriteAsync(changed scopes only)   ◄── atomic: only on success
    └─ 6. Clear the ambient accessor (finally)
```

Because the middleware sets the ambient `TurnState` **before** calling `next` and clears it in a `finally`, and because `Context` is created downstream inside `next`, `context.State` resolves to the correct per-turn instance without threading it through every constructor. `AsyncLocal<T>` flows with the async pipeline that `TurnMiddleware.RunPipelineAsync` already uses, so this is concurrency-safe across simultaneous turns.

If an exception propagates out of `next`, step 5 is skipped — **no writes occur**. This is the atomic-save guarantee, and it falls out naturally from the pipeline's existing exception flow (`BotApplication.ProcessAsync` wraps the pipeline in its `try/catch`).

### Relationship to existing components

```
StateMiddleware (Apps)  ── implements ──►  ITurnMiddleware (Core)
        │
        ├── IStorage.ReadAsync / WriteAsync / DeleteAsync   → backend I/O
        ├── TurnState.DeriveKeys                            → key derivation from TeamsActivity
        └── TurnState                                       → per-turn document, change tracking

Context<TActivity> (Apps)
        └── State  ──►  reads the ambient TurnState published by StateMiddleware
```

`StateMiddleware` does **not** replace `IStorage` — it orchestrates a storage backend, key derivation, and the three scopes into one load/save envelope and exposes the result on `Context`.

## Scopes

`TurnState` provides three scopes, each with a different lifetime and persistence model.

| Scope | Lifetime | Persists? | Storage key (logical) | Use case |
|---|---|---|---|---|
| **Conversation** | Entire conversation (all turns, all participants) | ✅ Yes | `{channelId}/conversations/{conversationId}` | Dialog state, turn counter, shared conversation data |
| **User** | Across all conversations with this user | ✅ Yes | `{channelId}/users/{fromId}` | User preferences, display name, settings |
| **Temp** | Current turn only | ❌ No | (none) | Scratch space; pass data between middleware and handlers |

Keys are derived from the inbound `CoreActivity`:

- `channelId` ← `activity.ChannelId`
- `conversationId` ← `activity.Conversation.Id`
- `fromId` ← `activity.From.Id`

Including `channelId` in both keys prevents state from leaking across channels/tenants. Key derivation is a single seam (`TurnState.DeriveKeys`) so the scheme can evolve without touching handlers.

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

### Temp scope

`Temp` is never persisted — it is discarded at the end of the turn. Useful for passing data from middleware to a handler.

```csharp
// Middleware writes to temp
bot.UseMiddleware(new DelegateMiddleware(async (botApp, activity, next, ct) =>
{
    TurnState.Current?.Temp.Set("requestId", Guid.NewGuid().ToString());
    await next(ct);
}));

// Handler reads it
bot.OnMessage(async (ctx, ct) =>
{
    var requestId = ctx.State?.Temp.Get<string>("requestId") ?? "unknown";
    await ctx.SendAsync($"Request: {requestId}", ct);
});
```

## API Surface

### Registration

State is registered two ways — DI options (recommended) or the `App.Builder()` fluent builder. Both set the storage on `TeamsBotApplicationOptions`; the `StateMiddleware` is added (via `UseMiddleware`) when the `TeamsBotApplication` is constructed.

**DI options (recommended):**

```csharp
builder.Services.AddTeamsBotApplication(options =>
{
    options.UseState(new MemoryStorage());
});
```

**Fluent `App.Builder()`** (mirrors `AddOAuth`):

```csharp
App.Builder().UseState(new MemoryStorage());
```

The two entry points:

```csharp
public sealed class TeamsBotApplicationOptions
{
    /// Register state; a StateMiddleware is added when the bot is constructed.
    public TeamsBotApplicationOptions UseState(IStorage storage);
}

public class AppBuilder
{
    /// Fluent equivalent; delegates to TeamsBotApplicationOptions.UseState.
    public AppBuilder UseState(IStorage storage);
}
```

Registering twice keeps the last storage (last-write-wins, single instance).

### `IStorage`

```csharp
namespace Microsoft.Teams.Core.State;

/// <summary>
/// Backing store for <see cref="TurnState"/>. Implementations persist opaque
/// state documents keyed by string. All members must be safe to call concurrently.
/// </summary>
public interface IStorage
{
    /// Read the documents for the given keys. Missing keys are omitted from the result.
    Task<IReadOnlyDictionary<string, StoreItem>> ReadAsync(
        IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default);

    /// Write the given documents. Implementations should honor optimistic concurrency
    /// via <see cref="StoreItem.ETag"/> when supported; v1 backends use last-write-wins.
    Task WriteAsync(
        IReadOnlyDictionary<string, StoreItem> changes, CancellationToken cancellationToken = default);

    /// Delete the documents for the given keys.
    Task DeleteAsync(
        IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default);
}

/// <summary>A persisted state document: its JSON payload plus an optional concurrency tag.</summary>
public sealed class StoreItem
{
    /// The serialized state values for a single scope.
    public IDictionary<string, object?> Values { get; init; } = new Dictionary<string, object?>();

    /// Optional concurrency token (reserved for future optimistic-concurrency support).
    public string? ETag { get; set; }
}
```

`ReadAsync`/`WriteAsync` take **collections of keys** (not a single key) so the middleware can hydrate the Conversation and User scopes in one round-trip, and so backends can pipeline the operations.

### `TurnState` and scopes

```csharp
namespace Microsoft.Teams.Core.State;

public sealed class TurnState
{
    /// Per-conversation persisted scope.
    public StateScope Conversation { get; }

    /// Per-user persisted scope.
    public StateScope User { get; }

    /// Per-turn, non-persisted scope.
    public StateScope Temp { get; }

    /// Ambient TurnState for the current turn, published by StateMiddleware. Null when
    /// state middleware is not registered or accessed outside a turn.
    public static TurnState? Current { get; }

    /// True once the turn has completed and state has been saved. After this, every scope
    /// read/write throws (see "Lifetime and after-turn access").
    public bool IsCompleted { get; }

    /// Path access: "conversation.count", "user.name", "temp.x", or bare "x" (defaults to temp).
    public T? GetValue<T>(string path);
    public void SetValue<T>(string path, T value);
}

// Concrete sealed class — the three scopes are instances of it (persisted vs temp is a
// constructor flag), so no interface is needed.
public sealed class StateScope
{
    // All four members throw InvalidOperationException if the owning turn has completed.
    public T? Get<T>(string key);
    public void Set<T>(string key, T value);
    public bool Remove(string key);
    public bool ContainsKey(string key);
}
```

`Get<T>` reuses the same `JsonElement` round-trip logic as `ExtendedPropertiesDictionary.Get<T>` (caching the deserialized value back into the dictionary), so deserialization happens once per key per turn.

### `Context<TActivity>.State`

```csharp
public class Context<TActivity> where TActivity : TeamsActivity
{
    /// The state for the current turn, or null if state middleware is not registered.
    public TurnState? State { get; }
}
```

`State` is a thin pass-through to `TurnState.Current` resolved from the ambient accessor, so it stays consistent whether the developer reads it from a route handler, a middleware, or an OAuth callback.

### Storage adapters

```csharp
// In-process. Thread-safe (ConcurrentDictionary). Lost on restart.
public sealed class MemoryStorage : IStorage { public MemoryStorage(); }

// JSON files on disk. Single-instance only. Default directory "./bot-state".
public sealed class FileStorage : IStorage
{
    public FileStorage();                 // "./bot-state"
    public FileStorage(string directory);
}

// Opt-in package Microsoft.Teams.State.Redis. Multi-instance safe.
public sealed class RedisStorage : IStorage, IAsyncDisposable
{
    public RedisStorage(string connectionString, string keyPrefix = "teams:state:");
    public ValueTask DisposeAsync();
}
```

## Internal Flow

### Turn load/save sequence

```
ProcessAsync(httpContext)
    │  (existing) deserialize CoreActivity, begin logging scope
    │
    └─ MiddleWare.RunPipelineAsync(botApp, activity, OnActivity, 0, token)
         │
         ├─ ... earlier middleware ...
         │
         └─ StateMiddleware.OnTurnAsync(botApp, activity, next, ct)
              │
              ├─ 1. keys = TurnState.DeriveKeys(activity)
              │        convKey = $"{channelId}/conversations/{conversationId}"
              │        userKey = $"{channelId}/users/{fromId}"
              │        (skip a scope whose key parts are missing; that scope is read-only/empty)
              │
              ├─ 2. items = await storage.ReadAsync([convKey, userKey], ct)
              │
              ├─ 3. turnState = new TurnState(
              │        conversation: Scope.Hydrate(items[convKey]),
              │        user:         Scope.Hydrate(items[userKey]),
              │        temp:         Scope.Empty())
              │     loadHash[convKey] = Hash(turnState.Conversation)   // baseline for change detection
              │     loadHash[userKey] = Hash(turnState.User)
              │     _ambient.Value = turnState          // AsyncLocal publish
              │
              ├─ 4. await next(ct)                       // OnActivity builds Context, dispatches routes
              │
              ├─ 5. // only reached when next() did NOT throw
              │     var changes = {} ; var deletes = []
              │     // re-serialize each persisted scope and compare to its load-time hash
              │     foreach persisted scope that changed:
              │         scope.IsEmpty ? deletes.Add(key)            // emptied this turn → remove the row
              │                       : changes[key] = scope.ToStoreItem()
              │     if (changes.Count > 0) await storage.WriteAsync(changes, ct)
              │     if (deletes.Count > 0) await storage.DeleteAsync(deletes, ct)
              │     // Temp is never written
              │
              └─ finally:
                    turnState.Complete()     // seal: later scope access throws (see below)
                    _ambient.Value = null
```

**Atomicity** comes from step 5 sitting *after* the `await next(ct)`: if the handler throws, control never reaches the write, and `BotApplication.ProcessAsync`'s existing `catch` wraps and rethrows as `BotHandlerException`. No partial writes.

**Write-only-if-changed** — each persisted scope is serialized at load to capture a baseline hash and re-serialized at save; only scopes whose hash differs are written. This catches in-place mutation of a fetched reference type (`Get<List<string>>(...).Add(...)`) that a dirty flag would silently miss, avoids a round-trip on unchanged turns, and keeps `Temp` out of the backend. Cost is one extra serialization per persisted scope per turn — the same trade Bot Framework, Teams AI, and the Agents SDK all make.

**Delete-on-empty** — a changed scope that ends the turn empty (e.g. `scope.Clear()`, or removing its last value) is routed to `storage.DeleteAsync` instead of writing an empty document, so a cleared scope leaves no orphan row. Because the delete only fires when the scope *changed* to empty, a scope that was always empty never issues a spurious delete.

### Lifetime and after-turn access

`TurnState` is **scoped to a single turn**. It is loaded at the start of the turn and **sealed** at the end (`turnState.Complete()` in the middleware's `finally`, step 6). Accessing it after the turn — almost always by capturing `ctx.State` in fire-and-forget background work that outlives the turn — is a misuse, because the atomic save in step 5 has already run and will not run again.

A captured reference does not become `null` (the object is still alive, and with `AsyncLocal` an in-flight `Task.Run` keeps the snapshot it captured), so a naive design would let the background code read a **stale** copy and write changes that **silently never persist**. To make that misuse loud instead of silent, every scope read/write checks `IsCompleted` and throws a descriptive error once the turn has ended:

```csharp
// inside StateScope.Get/Set/Remove
if (_owner.IsCompleted)
    throw new InvalidOperationException(
        "TurnState was accessed after the turn completed. State is per-turn and is saved " +
        "once when the handler returns. Capture the values you need before starting background " +
        "work, e.g. `var name = ctx.State.User.Get<string>(\"name\");`.");
```

This throws regardless of whether the captured ambient is stale or null, and it fires *through* the idiomatic `ctx.State?.` access (because `State` is non-null — it is the scope method that throws), so the failure cannot be swallowed by the null-conditional operator. The correct pattern is to read the values out **during** the turn and pass those into the background work:

```csharp
bot.OnMessage(async (ctx, ct) =>
{
    string? name = ctx.State?.User.Get<string>("name");   // read now, during the turn
    _ = Task.Run(async () =>
    {
        await SomeSlowCallAsync(name);                     // use the captured value, not ctx.State
    });
});
```

### Key derivation

```
TurnState.DeriveKeys(TeamsActivity activity)
    │
    ├─ channelId       = activity.ChannelId        (required for any persisted scope)
    ├─ conversationId  = activity.Conversation?.Id
    ├─ fromId          = activity.From?.Id
    │
    ├─ conversationKey = (channelId is null || conversationId is null)
    │                       ? null                         // conversation scope unavailable
    │                       : $"{channelId}/conversations/{conversationId}"
    │
    └─ userKey         = (channelId is null || fromId is null)
                            ? null                          // user scope unavailable
                            : $"{channelId}/users/{fromId}"
```

When a key is null (e.g. an activity with no `From`), that scope is hydrated empty and never written — reads return defaults and writes are silently dropped, so handlers never need null-guards beyond the existing `ctx.State?.` pattern.

### Serialization

A state scope is an **open-typed bag** — `Dictionary<string, object?>` of whatever POCOs the developer stores. Those types can't be enumerated at build time, so unlike the activity pipeline (a closed set of known types), state serialization is fundamentally reflection-based. Rather than stand up a parallel, state-specific source-gen context — which would give a *false* impression of an AOT fast path while every `object`-typed value falls through to reflection anyway — `StateSerializer` reuses the canonical `TeamsActivityJsonContext`:

```csharp
internal static readonly JsonSerializerOptions Options = new()
{
    // Teams context supplies source-generated metadata for the primitives and JsonElement values
    // that commonly appear; the reflection resolver handles arbitrary user POCO values.
    TypeInfoResolver = JsonTypeInfoResolver.Combine(TeamsActivityJsonContext.Default, new DefaultJsonTypeInfoResolver()),
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};
```

Values written via `Set<T>(key, value)` are held as boxed objects in-memory and serialized lazily on save; values read via `Get<T>` deserialize the stored `JsonElement` on first access and cache the typed result — identical to the `ExtendedPropertiesDictionary.Get<T>` behavior already in Core. Cross-runtime interop (a Redis document written by a .NET bot read by a Node/Python bot) is preserved by using camelCase JSON with no .NET-specific type markers.

## Path Syntax (Advanced)

Instead of scope properties, a path string can be used:

| Path | Equivalent |
|---|---|
| `"conversation.count"` | `state.Conversation.Get<int>("count")` |
| `"user.name"` | `state.User.Get<string>("name")` |
| `"temp.requestId"` | `state.Temp.Get<string>("requestId")` |
| `"foo"` | `state.Temp.Get<object>("foo")` (defaults to temp) |

```csharp
ctx.State?.SetValue("conversation.count", 5);   // same as Conversation.Set("count", 5)
ctx.State?.SetValue("foo", "bar");              // same as Temp.Set("foo", "bar")
```

## Atomic Semantics

State is **saved only when the turn handler completes successfully**. If the handler throws, **state mutations are discarded** — nothing is written to storage. This guarantees that a failed turn never leaves persisted state in a partially-updated, invalid condition.

```csharp
bot.OnMessage(async (ctx, ct) =>
{
    ctx.State?.Conversation.Set("count", 42);

    if (ctx.Activity.Text == "crash")
        throw new InvalidOperationException("Simulated error");   // count is NOT persisted

    await ctx.SendAsync("OK", ct);                                // success → count persists
});
```

This is implemented by the middleware writing only after `await next(ct)` returns normally (see [Internal Flow](#internal-flow)).

## Storage Adapters

### MemoryStorage (Core)

- **Thread-safe**: Yes (`ConcurrentDictionary`).
- **Persistence**: None (lost on restart).
- **Use case**: Development, testing, stateless hosts.

### FileStorage (Core)

- **Thread-safe**: No — single-instance only.
- **Persistence**: One JSON file per key on disk.
- **Use case**: Local development, single-instance deployments.
- **Limitations**: ⚠️ Two instances pointing at the same directory will overwrite each other's state. No built-in cleanup.

### RedisStorage (opt-in package `Microsoft.Teams.State.Redis`)

- **Thread-safe**: Yes (Redis serializes operations).
- **Persistence**: Configurable via Redis (RDB / AOF / none).
- **Use case**: Multi-instance / horizontally scaled deployments; state shared across replicas.
- **Packaging**: `dotnet add package Microsoft.Teams.State.Redis`. Never pulled into the core install — same opt-in philosophy noted for the distributed dedup store in [OAuthFlow-Design.md](sso/OAuthFlow-Design.md).
- **Key encoding**: `{keyPrefix}{logicalKey}` (default prefix `teams:state:`). Redis is binary-safe; no escaping needed.
- **Cluster compatibility**: Multi-key reads/writes are issued as pipelined single-key commands (not `MGET` / multi-key `DEL`) to avoid `CROSSSLOT` errors on Redis Cluster.
- **Lifecycle**: `IAsyncDisposable` — dispose on shutdown (`await using` or DI-managed disposal).

### Cloud adapters (future)

`BlobStorage` (Azure Blob) and `CosmosDbStorage` (Azure Cosmos DB) are deferred to a later version, following the same `IStorage` contract.

## File Placement

| File | Location |
|---|---|
| `IStorage.cs` | `Microsoft.Teams.Apps/State/IStorage.cs` |
| `StoreItem.cs` | `Microsoft.Teams.Apps/State/StoreItem.cs` |
| `TurnState.cs` | `Microsoft.Teams.Apps/State/TurnState.cs` |
| `StateScope.cs` | `Microsoft.Teams.Apps/State/StateScope.cs` |
| `StateMiddleware.cs` | `Microsoft.Teams.Apps/State/StateMiddleware.cs` |
| `MemoryStorage.cs` | `Microsoft.Teams.Apps/State/MemoryStorage.cs` |
| `StateSerializer.cs` | `Microsoft.Teams.Apps/State/StateSerializer.cs` |
| `FileStorage.cs` | `Microsoft.Teams.Apps/State/FileStorage.cs` (future) |
| `RedisStorage.cs` | `Microsoft.Teams.State.Redis/RedisStorage.cs` (opt-in package, references Apps) |

`UseState` lives on `TeamsBotApplicationOptions` and `AppBuilder`; the `StateMiddleware` is registered in the `TeamsBotApplication` constructor. No separate `StateExtensions` file.

## Changes to Core / Apps

| File | Change |
|---|---|
| `Context.cs` | Add `public TurnState? State { get; }` returning the ambient `TurnState.Current`. |
| `TeamsBotApplicationOptions.cs` | Add `UseState(IStorage storage)` and an internal `IStorage? StateStorage` descriptor; construct + register `StateMiddleware` in the `TeamsBotApplication` constructor (next to the OAuth flow loop). |
| `AppBuilder.cs` | Add `UseState(IStorage storage)` delegating to `Options.UseState`. |
| `BotApplication.cs` | No change required — `UseMiddleware` already exists; `StateMiddleware` registers through it. |

## Edge Cases & Constraints

| Scenario | Behavior |
|---|---|
| State middleware not registered | `context.State` is `null`. Bot behaves exactly as before (fully opt-in). |
| Activity has no `From` (e.g. some conversationUpdate) | User scope key is null → user scope is empty and never written. Conversation scope still works. |
| Activity has no `Conversation` | Conversation scope is empty and never written. |
| Handler throws | No writes occur (atomic). `BotApplication.ProcessAsync` wraps the exception as `BotHandlerException`. |
| Unchanged turn (no mutation) | No backend write — each scope's save-time hash matches its load-time hash. |
| Concurrent turns in the same conversation | Last-write-wins in v1 (no optimistic concurrency). `StoreItem.ETag` is reserved for a future version. |
| Large value stored | Allowed but discouraged — `TurnState` is not a blob store. Keep documents to a few KB. Use Blob/file storage for large payloads. |
| `Temp` value | Never persisted; discarded when the turn ends (the ambient is cleared in `finally`). |
| Accessing `TurnState.Current` outside any turn | Returns `null` (the `AsyncLocal` is only set for the duration of the pipeline). |
| Accessing state **after the turn** (captured `ctx.State` in background work) | Scope reads/writes throw a descriptive `InvalidOperationException` once the turn is sealed (`IsCompleted`). The throw fires through `ctx.State?.` and is not affected by whether the captured ambient is stale or null. Capture values during the turn instead (see [Lifetime and after-turn access](#lifetime-and-after-turn-access)). |
| `FileStorage` across two instances | Unsafe — instances overwrite each other. Use `RedisStorage` for multi-instance. |
| Sensitive values | Stored as plain JSON. No encryption in v1 — encrypt before `Set` if required. |
| Cross-runtime document sharing | Supported via camelCase JSON with no .NET-specific markers; a Redis document is interoperable across .NET / Node / Python bots. |

## What TurnState is NOT

- **Not a database** — optimized for small per-conversation / per-user key-value documents. For queries, use a real database.
- **Not for large blobs** — serializes to JSON; store small objects. Large files belong in Blob storage.
- **Not a distributed lock** — last-write-wins in v1; do not use for coordination.
- **Not a web session store** — it is bot-turn-scoped, not HTTP-session-scoped.

## Limitations in v1

- **No concurrency control** — simultaneous writes to the same key are last-write-wins; `StoreItem.ETag` is reserved but not enforced.
- **Adapters** — v1 ships `MemoryStorage`, `FileStorage` (Core) and `RedisStorage` (opt-in package). `BlobStorage` / `CosmosDbStorage` are deferred.
- **`FileStorage` is single-instance only** — use `RedisStorage` for scaled deployments.
- **No encryption** — values are plain JSON.
- **No expiration** — state persists until explicitly deleted; implement manual cleanup if needed.

## See Also

- [Architecture.md](Architecture.md) — how the three layers and the middleware pipeline fit together.
- [OAuthFlow-Design.md](sso/OAuthFlow-Design.md) — the layering / opt-in-package pattern this design mirrors.
- [Activity-Design.md](Activity-Design.md) — the `CoreActivity` schema state keys are derived from.
