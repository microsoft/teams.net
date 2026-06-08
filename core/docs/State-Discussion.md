# Turn State — Design & Implementation Walkthrough

> **Format:** paste this page into a Microsoft Loop page/component. Headings, tables, code blocks,
> callouts (`>`) and checklists render natively.
>
> **Audience:** engineers reviewing the `TurnState` feature in `Microsoft.Teams.Apps`.
> **Time:** ~30 min. **Goal:** understand the model *and* the tradeoffs behind each decision.

## TL;DR — the throughline

> **State is small, per-conversation / per-user, last-writer-safe key-value — and every design choice
> (cache backend, two scopes, no ETag, no history) follows from honoring that scope and refusing to be a
> database.** If anyone pushes on a decision, that sentence is the anchor.

## Agenda

| # | Topic | Min | Key question to put to the room |
|---|-------|-----|---------------------------------|
| 1 | What problem does state solve? | 3 | What boilerplate are we deleting? |
| 2 | API & scope model | 4 | Why only two scopes (no `Temp`)? |
| 3 | **Storage abstraction (centerpiece)** | 7 | Is a *cache* the right contract for state? |
| 4 | Concurrency, atomicity & pitfalls | 6 | When do you actually need OCC? Where's the DB line? |
| 5 | Lifecycle & plumbing | 5 | Ambient vs. explicit threading? |
| 6 | Serialization | 2 | Why no honest AOT fast path here? |
| 7 | DI wiring & defaults | 2 | Keep `options` pure config? |
| 8 | Testing & ops + Q&A | 1+ | — |

> **15-min cut:** do **3 + 4 + 5** only; 1–2 become a 3-min intro; 6–8 become "ask me after."

---

## 1 · What problem does state solve?

Without it, every handler re-implements: derive a stable key from `ChannelId` / `Conversation.Id` /
`From.Id`, pick a backend, read-mutate-write **and remember not to write on failure**, plus the
`JsonElement` round-trip. State reduces that to:

```csharp
var count = ctx.State?.Conversation.Get<int>("count") ?? 0;
ctx.State?.Conversation.Set("count", count + 1);   // key derivation, change tracking, save = framework's job
```

---

## 2 · API & scope model

Opt in once; read through `ctx.State` (null when not enabled, so `?.` is the idiom).

```csharp
builder.Services.AddTeamsBotApplication(o => o.UseState());

bot.OnMessage(async (ctx, ct) =>
{
    ctx.State?.User.Set("name", "Ada");
    string? name = ctx.State?.User.Get<string>("name");
});
```

| Scope | Lifetime | Persists? | Logical key |
|-------|----------|-----------|-------------|
| `Conversation` | whole conversation (all participants, all turns) | ✅ | `{channelId}/conversations/{conversationId}` |
| `User` | across all conversations for a user | ✅ | `{channelId}/users/{fromId}` |

> **Discuss:** We **removed a transient `Temp` scope**. Is per-turn scratch space the framework's job, or
> does `Context` already cover it? (Minimalism vs. convenience.) `channelId` is in *both* keys — why?
> (Prevents cross-tenant/channel leakage.)

---

## 3 · Storage abstraction — the centerpiece

**Decision:** back state with the standard .NET `IDistributedCache`, not a bespoke `IStorage`.

```csharp
// In-process by default (TryAdd) — works immediately:
builder.Services.AddTeamsBotApplication(o => o.UseState());

// Override for multi-instance — takes precedence over the default:
builder.Services.AddStackExchangeRedisCache(o => o.Configuration = "localhost:6379");
builder.Services.AddTeamsBotApplication(o => o.UseState());
```

| | For | Against / what you give up |
|---|-----|----------------------------|
| Backends | Redis, SQL Server, Garnet, NCache **for free**; same abstraction ASP.NET session uses | — |
| Expiration | `DistributedCacheEntryOptions` TTL for free | — |
| Semantics | — | It's a **cache**: backends may **evict** → silent data loss for "state" |
| Concurrency | — | No compare-and-swap → **no ETag / optimistic-concurrency path, ever** |
| Interop | — | Built-in `RedisCache` wraps values in a Redis **hash** → not cross-SDK plain-JSON readable |

> **Debate (spend time here):**
> 1. *"Is a cache the right contract for state?"* — eviction vs. durability.
> 2. *Adapter-over-`IStorage` vs. replace `IStorage` entirely* — we chose replace; the cost was ETag +
>    the guaranteed document format. Worth it?

---

## 4 · Concurrency, atomicity & pitfalls

**Atomic save** — only on handler success; an exception discards mutations:

```csharp
ctx.State?.Conversation.Set("count", 42);
if (ctx.Activity.Text == "crash")
    throw new InvalidOperationException();   // count is NOT persisted
await ctx.SendAsync("ok", ct);               // success → count persists
```

**Last-write-wins drift — the trap to teach app authors:**

```csharp
// ❌ DON'T: read-modify-write a growing collection under last-write-wins.
var history = ctx.State!.Conversation.Get<List<string>>("messages") ?? new();
history.Add(ctx.Activity.Text!);                 // two concurrent turns both load, append, write back —
ctx.State!.Conversation.Set("messages", history); // the later write clobbers the earlier → messages lost
```

> **Rule:** keep small, **last-writer-safe** values in state (a counter, a flag, the dialog step).
> Persist history in an **append-only** store (DB/Blob).
>
> **Discuss:** When do you genuinely need OCC, and what would you reach for instead? Where is the line
> between "state" and "a database"?

---

## 5 · Lifecycle & plumbing — how state reaches a handler

Load/save runs in `OnActivity`, around routing:

```text
OnActivity(activity):
  turnState = store.LoadAsync(activity)        // derive keys, GetAsync both scopes
  Context(this, teamsActivity, turnState)      // state passed in explicitly
  try   { dispatch routes; store.SaveAsync(turnState) }   // save only if no throw
  finally { turnState.Complete() }             // seal: later scope access throws
```

The `TurnState` is **threaded explicitly** through the `Context` constructor (no ambient). The per-route
`Context` forwards it:

```csharp
// Route.InvokeRoute — every matched route gets a fresh Context that shares the one TurnState:
Context<TActivity> typedContext = new(ctx.TeamsBotApplication, (TActivity)ctx.Activity, ctx.State);
```

**After-turn guard (`IsCompleted`) + fire-and-forget pattern:**

```csharp
string who = ctx.State!.User.Get<string>("name") ?? "there";   // capture DURING the turn

_ = Task.Run(async () =>
{
    await Task.Delay(TimeSpan.FromSeconds(2));

    if (ctx.State!.IsCompleted) { /* true — safe to check without tripping the guard */ }

    // ctx.State.User.Get(...) HERE throws InvalidOperationException (state is sealed).
    Console.WriteLine($"reminder for {who}");   // use the captured value
});
```

> **Debate — the carrier journey** (each rejected for a concrete reason):
> `AsyncLocal` ambient → activity-carried → **explicit constructor threading**.
> Ambient = ergonomic but "magic" and per-route `Context` reconstruction complicates it; explicit =
> verbose but testable and obvious. Which would *you* ship?

---

## 6 · Serialization

A scope is an open-typed bag (`Dictionary<string, object?>`), so reflection is unavoidable — there is no
honest fully-source-generated path. We reuse the canonical context + a reflection fallback, byte-native:

```csharp
TypeInfoResolver = JsonTypeInfoResolver.Combine(
    TeamsActivityJsonContext.Default,        // fast metadata for primitives / JsonElement
    new DefaultJsonTypeInfoResolver()),      // reflection for arbitrary user POCOs
PropertyNamingPolicy = JsonNamingPolicy.CamelCase,   // cross-runtime, no .NET type markers
```

> Values are stored as camelCase UTF-8 JSON; `Get<T>` lazily deserializes the `JsonElement` once per key.

---

## 7 · DI wiring & defaults

Store is a real DI singleton, **constructor-injected** (not service-located, not carried on `options`):

```csharp
// HostingExtensions — only when state is enabled:
if (teamsOptions.StateEnabled)
{
    services.AddDistributedMemoryCache();    // TryAdd → explicit Redis/SQL cache wins
    services.AddSingleton(sp =>
        new TurnStateStore(sp.GetRequiredService<IDistributedCache>(), teamsOptions.StateEntryOptions));
}

// TeamsBotApplication ctor — DI injects it (null = state off):
public TeamsBotApplication(..., TurnStateStore? stateStore = null) { _stateStore = stateStore; }
```

> **Discuss:** `options` stays **pure configuration** (a flag + TTL) — no live service riding on it. We
> rejected (a) cache-on-options + factory mutation and (b) service location in `ProcessAsync` to land here.

---

## 8 · Testing & ops

```text
In-process : send `count`, restart the bot → counter resets (not persisted).
Redis      : set ConnectionStrings:Redis, send `count`, restart → counter continues (persisted).
             KEYS * → msteams/conversations/...  and  msteams/users/...
```

> **Windows, no Docker?** Use **Garnet** (Microsoft's Redis-compatible .NET server — no install) or
> **Memurai** (`winget install Memurai.MemuraiDeveloper`). Both listen on `:6379`.
>
> **Caveat:** `IDistributedCache` may evict; use a durable backend with no aggressive eviction on state
> keys. `RedisCache` hash-wraps values — inspect `HGETALL <key>`, not `GET`.

---

## Decision log (one-liners)

| Decision | Choice | Why |
|----------|--------|-----|
| Backend | `IDistributedCache` | free backends + TTL; standard abstraction |
| Scopes | `Conversation` + `User` only | dropped `Temp` — not the framework's job |
| Concurrency | last-write-wins | `IDistributedCache` has no CAS; ETag dropped |
| History | **not** in state | drift under concurrent read-modify-write |
| Carrier | explicit `Context` ctor param | no ambient; testable, obvious |
| Store wiring | DI singleton, ctor-injected | `options` stays pure config |
| Default cache | in-process via `TryAdd` | `UseState()` works alone; Redis overrides |

## Run-of-show checklist

- [ ] Open with the throughline sentence
- [ ] Demo: `count` → restart (in-process) vs. Redis
- [ ] Anchor 7 min on the storage-abstraction debate (§3)
- [ ] Teach the drift pitfall (§4) — highest practical value
- [ ] Walk the carrier journey (§5) — best "why not the obvious thing" story
- [ ] Close: "state, not a database" — where's the line for *your* app?
