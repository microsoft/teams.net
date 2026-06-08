# Example: StateBot

A bot that demonstrates **turn state** — per-conversation and per-user values that load at the start of
each turn and save automatically when the handler succeeds. State is backed by `IDistributedCache`
(in-process by default, Redis when configured) and enabled with a single `options.UseState()`.

## Commands

| Command | Behavior |
|---------|----------|
| `count` | Increments a counter in **conversation** state (shared by everyone in the chat) |
| `my name is <name>` | Saves your name in **user** state (follows you across conversations) |
| `whoami` | Reads your name back from user state |
| `remind me` | Starts fire-and-forget work that outlives the turn — demonstrates `TurnState.IsCompleted` |
| `reset` | Clears this conversation's state (deletes the stored document) |
| `help` | Shows available commands |

## Run

```bash
dotnet run
```

By default, state is held in an in-process `IDistributedCache` — it works immediately but is **lost when
the process restarts**.

## Test state persistence

### In-process (default)

1. Send `count` a few times → the counter increments and survives across turns.
2. Send `my name is Ada`, then `whoami` from a different conversation → your name follows you.
3. **Restart the bot** and send `count` → the counter is back to `0` (in-process state did not survive).

### Redis (survives restart, shared across instances)

1. Start Redis:
   ```bash
   docker run --rm -p 6379:6379 redis
   ```
2. Provide a `Redis` connection string — either in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": { "Redis": "localhost:6379" }
   }
   ```
   or via an environment variable:
   ```bash
   ConnectionStrings__Redis=localhost:6379 dotnet run
   ```
   When the connection string is present, the sample registers `AddStackExchangeRedisCache`, which takes
   precedence over the in-process default.
3. Send `count` / `my name is Ada`, then inspect Redis:
   ```bash
   redis-cli KEYS '*'
   # → "msteams/conversations/<conversationId>"
   #   "msteams/users/<userId>"
   ```
4. **Restart the bot** and send `count` → the counter continues from where it left off (Redis persisted it).

> The built-in `RedisCache` wraps each value in a Redis **hash** (`data` / `absexp` / `sldexp`), so
> `redis-cli HGETALL <key>` shows the document under `data` rather than as a top-level plain string. This
> document is therefore not directly readable by the Node/Python SDKs — fine for a .NET-only sample.

## Notes

- **Two scopes.** `context.State.Conversation` (per conversation) and `context.State.User` (per user). Both
  persist; there is no transient/temp scope. State is opt-in — `context.State` is `null` if `UseState()`
  was not called, so handlers use `context.State?.…`.
- **Atomic save.** State saves only when the handler returns without throwing; a failed turn discards changes.
- **After-turn access (`IsCompleted`).** `TurnState` is sealed when the turn ends. The `remind me` command
  shows the correct fire-and-forget pattern: read the values you need **during** the turn and pass those
  into background work. `State.IsCompleted` lets background code check the turn ended without tripping the
  guard; touching a sealed scope (`State.User.Get(...)`) after the turn throws `InvalidOperationException`.
