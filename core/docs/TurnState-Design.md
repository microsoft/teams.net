# Turn State Design

## Overview

Enable per-turn state management in `BotApplication`, backed by `IDistributedCache` from the ASP.NET ecosystem. This allows developers to use any session state provider (Redis, SQL Server, in-memory, etc.) to persist state scoped to conversations and users.

## State Scopes

The state loader manages two independent state scopes per turn:

| Scope | Cache Key | Shared by |
|-------|-----------|-----------|
| **ConversationState** | `ts:conv:{Conversation.Id}` | All users in the conversation |
| **UserState** | `ts:user:{Conversation.Id}:{From.Id}` | Single user in a specific conversation |

Both scopes use the `TurnState` class. They are loaded/saved independently and have separate dirty tracking.

## Apps Layer (`Microsoft.Teams.Apps`)

### `TurnState`

Per-turn state storage backed by `Dictionary<string, object?>`. Tracks dirty state so the loader only writes back when something changed.

- Key-value entries stored under their string key.
- Typed objects stored under `$FullTypeName` (e.g. `$MyApp.UserPreferences`).
- Serialized to/from JSON via `System.Text.Json`.
- Handles `JsonElement` values from deserialization — both key-value `Get<T>(key)` and typed `Get<T>()` deserialize `JsonElement` to the requested type after a cache round-trip.

### `TurnStateContainer`

Holds the two state scopes for a turn:

```csharp
public sealed class TurnStateContainer
{
    public TurnState ConversationState { get; }
    public TurnState? UserState { get; }
}
```

`UserState` is null when the activity has no `From` field.

### `TurnStateLoader`

Loads and saves per-turn state from a distributed cache. Injected into `TeamsBotApplication` via constructor:

1. `LoadAsync`: Load `ConversationState` from cache key `ts:conv:{conversationId}`. If `From.Id` is present, load `UserState` from `ts:user:{conversationId}:{userId}`.
2. `SaveAsync`: Save each scope independently if dirty.
3. `DeleteAsync`: Remove conversation and/or user state from the cache.

State is loaded at the start of `OnActivity` and saved in a `finally` block, ensuring dirty state is persisted even on handler errors.

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

### DI Registration

`AddTeamsBotApplicationState` (private, called when `WithState()` is set) registers `TurnStateLoader`, options, and a default in-memory `IDistributedCache` via `AddDistributedMemoryCache()` (which uses `TryAdd`). This means `WithState()` works out of the box with no additional configuration.

When the developer registers a persistent provider (e.g. `AddStackExchangeRedisCache`), it takes precedence because it uses `Add` (not `TryAdd`), so the last registration wins in DI resolution regardless of call order.

### Cache Provider Warning

At construction, `TurnStateLoader` checks the resolved `IDistributedCache` implementation. If it is `MemoryDistributedCache`, a warning is logged:

> `Turn state is using the in-memory cache. State will be lost on restart. Register a persistent IDistributedCache (e.g. AddStackExchangeRedisCache) for production use.`

The warning disappears when a persistent provider is registered.

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

`WithState()` sets a flag on `TeamsBotApplicationOptions` that `AddTeamsBotApplication` reads to call `AddTeamsBotApplicationState()`. This keeps state configuration alongside OAuth and other bot options.

### `Context<TActivity>.State`

Returns the `TurnStateContainer` for the current turn. Set by `TeamsBotApplication.OnActivity` after loading state from the cache.

Follows the same pattern as `Context.Api` (accessor that throws if prerequisite isn't configured).

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
- `TurnStateContainer` can be constructed with test `TurnState` instances.
- No mocks needed for unit tests that just read/write state.

## File Layout

```
src/Microsoft.Teams.Apps/
├── State/
│   ├── TurnState.cs
│   ├── TurnStateContainer.cs
│   ├── TurnStateLoader.cs
│   └── TurnStateOptions.cs
├── TeamsBotApplication.cs              (state load/save in OnActivity)
├── TeamsBotApplication.HostingExtensions.cs  (AddTeamsBotApplicationState)
├── TeamsBotApplicationOptions.cs       (WithState fluent API)
└── Context.cs                          (State property)

samples/StateBot/                       (Redis-backed example)
```
