# Turn State Design

## Overview

Enable per-turn state management in `BotApplication`, backed by `IDistributedCache` from the ASP.NET ecosystem. This allows developers to use any session state provider (Redis, SQL Server, in-memory, etc.) to persist state scoped to conversations and users.

## State Scopes

The middleware manages two independent state scopes per turn:

| Scope | Cache Key | Shared by |
|-------|-----------|-----------|
| **ConversationState** | `ts:conv:{Conversation.Id}` | All users in the conversation |
| **UserState** | `ts:user:{Conversation.Id}:{From.Id}` | Single user in a specific conversation |

Both scopes use the same `ITurnState` interface and `TurnState` implementation. They are loaded/saved independently and have separate dirty tracking.

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

### `TurnStateContainer`

Holds the two state scopes for a turn:

```csharp
public sealed class TurnStateContainer
{
    public ITurnState ConversationState { get; }
    public ITurnState? UserState { get; }
}
```

`UserState` is null when the activity has no `From` field.

### `TurnStateMiddleware : ITurnMiddleware`

Manages state lifecycle within the middleware pipeline:

1. Extract `Conversation.Id`. If null/empty, skip state entirely and call `nextTurn()`.
2. Load `ConversationState` from cache key `ts:conv:{conversationId}`.
3. If `From.Id` is present, load `UserState` from cache key `ts:user:{conversationId}:{userId}`.
4. Set `botApplication.State = new TurnStateContainer(conversationState, userState)`.
5. `await nextTurn()`.
6. In `finally` block: save each scope independently if dirty. Always clear `botApplication.State`.

Dirty state is saved even if `nextTurn()` throws, ensuring no data loss on handler errors.

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
public TurnStateContainer? State { get; internal set; }
```

Set by `TurnStateMiddleware` at turn start, cleared at turn end. `internal set` restricts assignment to framework code.

### DI Registration (Core)

Extension method on `IServiceCollection`:

```csharp
public static IServiceCollection AddBotApplicationState(
    this IServiceCollection services,
    Action<TurnStateOptions>? configure = null)
```

Registers `TurnStateMiddleware`, options, and a default in-memory `IDistributedCache` via `AddDistributedMemoryCache()` (which uses `TryAdd`). This means `WithState()` works out of the box with no additional configuration.

When the developer registers a persistent provider (e.g. `AddStackExchangeRedisCache`), it takes precedence because it uses `Add` (not `TryAdd`), so the last registration wins in DI resolution regardless of call order.

### Cache Provider Warning

At startup, `UseBotApplication` checks the resolved `IDistributedCache` implementation. If it is `MemoryDistributedCache`, a warning is logged:

> `Turn state is using the in-memory cache. State will be lost on restart. Register a persistent IDistributedCache (e.g. AddStackExchangeRedisCache) for production use.`

The warning disappears when a persistent provider is registered.

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

Returns the `TurnStateContainer` for the current turn:

```csharp
public TurnStateContainer State => TeamsBotApplication.State
    ?? throw new InvalidOperationException(
        "State is not available. Call AddBotApplicationState() / WithState() during service registration.");
```

Follows the same pattern as `Context.Api` (lazy accessor that throws if prerequisite isn't configured).

### Usage

```csharp
// Conversation-scoped state (shared by all users)
int count = ctx.State.ConversationState.Get<int>("counter");
ctx.State.ConversationState.Set("counter", count + 1);

// User-scoped state (private per user per conversation)
var prefs = ctx.State.UserState?.Get<UserPreferences>() ?? new UserPreferences();
prefs.Theme = "dark";
ctx.State.UserState?.Set(prefs);
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
    // Conversation-scoped counter
    int counter = ctx.State.ConversationState.Get<int>("counter");
    counter++;
    ctx.State.ConversationState.Set("counter", counter);
    await ctx.SendActivityAsync($"Message #{counter} in this conversation.", ct);

    // User-scoped preferences
    var prefs = ctx.State.UserState?.Get<UserPrefs>() ?? new UserPrefs();
    prefs.Name = ctx.Activity.From?.Name ?? "anon";
    ctx.State.UserState?.Set(prefs);
});

app.Run();
```

## Serialization

`TurnState` serializes to/from JSON using `System.Text.Json`. The cache stores a single JSON object per session key. Each dictionary entry is a key-value pair; typed objects are stored under a `$TypeName` key.

## TTL / Expiration

Delegated entirely to `IDistributedCache` via `DistributedCacheEntryOptions` configured in `TurnStateOptions`. No framework-level TTL logic.

## Testing

- `TurnState` can be instantiated directly without DI or `IDistributedCache`.
- `TurnStateContainer` can be constructed with test `TurnState` instances and assigned to `BotApplication.State`.
- `internal set` on `BotApplication.State` is accessible to test projects via `InternalsVisibleTo`.
- No mocks needed for unit tests that just read/write state.

## File Layout

```
src/Microsoft.Teams.Core/
├── State/
│   ├── ITurnState.cs
│   ├── TurnState.cs
│   ├── TurnStateContainer.cs
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
