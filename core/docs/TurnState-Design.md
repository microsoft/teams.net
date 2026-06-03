# Turn State Design

## Overview

Enable per-turn state management in `BotApplication`, backed by `IDistributedCache` from the ASP.NET ecosystem. This allows developers to use any session state provider (Redis, SQL Server, in-memory, etc.) to persist state scoped to a conversation+user pair.

## Session Key

Derived from the incoming `CoreActivity`:

```
ts:{Conversation.Id}:{From.Id}
```

## Core Layer (`Microsoft.Teams.Core`)

### `ITurnState`

The contract for per-turn state access, supporting both key-value and typed object patterns:

```csharp
public interface ITurnState
{
    // Key-value access
    T? Get<T>(string key);
    void Set<T>(string key, T value);
    void Remove(string key);
    bool ContainsKey(string key);

    // Typed object access (keyed by type name)
    T Get<T>() where T : class, new();
    void Set<T>(T value) where T : class;
    bool Has<T>() where T : class;
    void Remove<T>() where T : class;

    // Dirty tracking
    bool IsDirty { get; }
}
```

### `TurnState`

Default implementation backed by `Dictionary<string, object?>`. Tracks dirty state so the middleware only writes back when something changed.

- Key-value entries stored under their string key.
- Typed objects stored under `$TypeName` (e.g. `$UserPreferences`).
- Serialized to/from JSON via `System.Text.Json`.
- Handles `JsonElement` values from deserialization — both key-value `Get<T>(key)` and typed `Get<T>()` deserialize `JsonElement` to the requested type after a cache round-trip.

### `TurnStateMiddleware : ITurnMiddleware`

Manages state lifecycle within the middleware pipeline:

1. Derive session key from `activity.Conversation.Id` + `activity.From.Id`.
2. Load serialized state from `IDistributedCache` (or create empty `TurnState` on cache miss).
3. Set `botApplication.TurnState = loadedState`.
4. `await nextTurn()`.
5. In `finally` block: if `state.IsDirty`, serialize and save back to `IDistributedCache` with configured entry options. Always clear `botApplication.TurnState`.
6. Dirty state is saved even if `nextTurn()` throws, ensuring no data loss on handler errors.

If `Conversation` or `From` is null (or their `Id` is null/empty), the middleware skips state loading and calls `nextTurn()` directly (some activity types like health checks may not carry these fields).

### `TurnStateOptions`

Configuration POCO:

```csharp
public class TurnStateOptions
{
    public DistributedCacheEntryOptions CacheEntryOptions { get; set; } = new()
    {
        SlidingExpiration = TimeSpan.FromHours(1)
    };
}
```

### `BotApplication` Change

One new property:

```csharp
public ITurnState? TurnState { get; internal set; }
```

Set by `TurnStateMiddleware` at turn start, cleared at turn end. `internal set` restricts assignment to framework code.

### DI Registration (Core)

Extension method on `IServiceCollection`:

```csharp
public static IServiceCollection AddBotApplicationState(
    this IServiceCollection services,
    Action<TurnStateOptions>? configure = null)
```

Registers `TurnStateMiddleware` and options. Does **not** register a default `IDistributedCache` — the developer must register their own provider (Redis, SQL, in-memory, etc.). This avoids silent fallback to in-memory when a real provider is intended, since `AddDistributedMemoryCache` uses `TryAdd` and would win over providers registered after it.

### Middleware Auto-Wiring

`UseBotApplication<TApp>()` checks for `TurnStateMiddleware` in the service provider and automatically adds it to the middleware pipeline if registered. No manual `UseMiddleware()` call needed.

## Apps Layer (`Microsoft.Teams.Apps`)

### `TeamsBotApplicationOptions.WithState()`

Fluent API for enabling state via the existing options pattern:

```csharp
// Default settings
services.AddTeamsBotApplication(options => options.WithState());

// Custom TTL
services.AddTeamsBotApplication(options =>
    options.WithState(state =>
        state.CacheEntryOptions.SlidingExpiration = TimeSpan.FromMinutes(30)));
```

`WithState()` sets a flag on `TeamsBotApplicationOptions` that `AddTeamsBotApplication` reads to call `AddBotApplicationState()`. This keeps state configuration alongside OAuth and other bot options.

### `Context<TActivity>.State`

Ergonomic accessor on the turn context:

```csharp
public ITurnState State => TeamsBotApplication.TurnState
    ?? throw new InvalidOperationException(
        "TurnState is not available. Call AddBotApplicationState() during service registration.");
```

Follows the same pattern as `Context.Api` (lazy accessor that throws if prerequisite isn't configured).

### Usage

```csharp
// Key-value
int count = context.State.Get<int>("counter");
context.State.Set("counter", count + 1);

// Typed object
var prefs = context.State.Get<UserPreferences>();
prefs.Theme = "dark";
context.State.Set(prefs);
```

No auto-save on typed objects. Developer mutates then calls `Set<T>()` to mark dirty.

### Full Example (Redis)

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddTeamsBotApplication(options => options.WithState());
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

var app = builder.Build();
var bot = app.UseTeamsBotApplication();

bot.OnMessage(async (ctx, ct) =>
{
    int counter = ctx.State.Get<int>("counter");
    counter++;
    ctx.State.Set("counter", counter);
    await ctx.SendActivityAsync($"Message #{counter}", ct);
});

app.Run();
```

## Serialization

`TurnState` serializes to/from JSON using `System.Text.Json`. The cache stores a single JSON object per session key. Each dictionary entry is a key-value pair; typed objects are stored under a `$TypeName` key.

## TTL / Expiration

Delegated entirely to `IDistributedCache` via `DistributedCacheEntryOptions` configured in `TurnStateOptions`. No framework-level TTL logic.

## Testing

- `TurnState` can be instantiated directly and assigned to `BotApplication.TurnState` without DI or `IDistributedCache`.
- `internal set` on `BotApplication.TurnState` is accessible to test projects via `InternalsVisibleTo`.
- No mocks needed for unit tests that just read/write state.

## File Layout

```
src/Microsoft.Teams.Core/
├── State/
│   ├── ITurnState.cs
│   ├── TurnState.cs
│   ├── TurnStateMiddleware.cs
│   └── TurnStateOptions.cs
├── Hosting/
│   └── AddBotApplicationExtensions.cs  (AddBotApplicationState + middleware auto-wiring)

src/Microsoft.Teams.Apps/
├── TeamsBotApplicationOptions.cs       (WithState fluent API)
├── TeamsBotApplication.HostingExtensions.cs  (wires WithState to AddBotApplicationState)
└── Context.cs                          (State property)

samples/StateBot/                       (Redis-backed example)
```
