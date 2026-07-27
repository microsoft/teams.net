# CachingAuthTokens

Demonstrates a Teams bot configured with a Redis-backed cache. It is intentionally tiny, but it shows the pattern for sharing state or auth-related data across turns instead of starting fresh each time.

## Prerequisites

- Redis available locally or through a connection string.
- A Teams bot registration.

## What it shows

- `AddStackExchangeRedisCache(...)` to plug Redis into the bot setup.
- A simple `OnMessage` handler that echoes the inbound text.
- Extra diagnostics that show timing and environment details so you can see the effect of the cache-backed setup.

---

## Behavior

| Message | Behavior |
|--------|----------|
| any text | Echoes the message and prints diagnostics |
| subsequent turns | Shows that the shared cache setup is in place |

---

It is useful when auth/session data needs to survive process restarts or be shared across multiple instances.
## Running the Sample

~~~bash
dotnet run --project samples/CachingAuthTokens/CachingAuthTokens.csproj
~~~
In Teams, exercise the commands/flows listed above to validate behavior.

