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

Default implementation backed by `Dictionary<string, object>`. Tracks dirty state so the middleware only writes back when something changed.

- Key-value entries stored under their string key.
- Typed objects stored under `$TypeName` (e.g. `$UserPreferences`).
- Serialized to/from JSON via `System.Text.Json`.

### `TurnStateMiddleware : ITurnMiddleware`

Manages state lifecycle within the middleware pipeline:

1. Derive session key from `activity.Conversation.Id` + `activity.From.Id`.
2. Load serialized state from `IDistributedCache` (or create empty `TurnState` on cache miss).
3. Set `botApplication.TurnState = loadedState`.
4. `await nextTurn()`.
5. If `state.IsDirty`, serialize and save back to `IDistributedCache` with configured entry options.
6. Clear `botApplication.TurnState`.

If `Conversation` or `From` is null, the middleware skips state loading and calls `nextTurn()` directly (some activity types like health checks may not carry these fields).

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

### DI Registration

Extension method on `IServiceCollection`:

```csharp
public static IServiceCollection AddBotApplicationState(
    this IServiceCollection services,
    Action<TurnStateOptions>? configure = null)
```

Registers `TurnStateMiddleware` and options. The developer is responsible for registering their `IDistributedCache` provider, same as ASP.NET session setup.

## Apps Layer (`Microsoft.Teams.Apps`)

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
context.State.Set("greeting_count", count + 1);
var count = context.State.Get<int>("greeting_count");

// Typed object
var prefs = context.State.Get<UserPreferences>();
prefs.Theme = "dark";
context.State.Set(prefs);
```

No auto-save on typed objects. Developer mutates then calls `Set<T>()` to mark dirty.

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
│   └── AddBotApplicationExtensions.cs  (AddBotApplicationState extension)

src/Microsoft.Teams.Apps/
└── Context.cs  (State property)
```
