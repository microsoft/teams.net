# StateBot

Shows conversation state and user state in a Teams bot, backed by Redis so values persist across turns.

## Prerequisites

- Bot registered and installed in Teams.
- Redis available and configured through `ConnectionStrings__Redis`.

## What it shows

- `UseState()` to enable Teams state helpers.
- Conversation state for a per-conversation message counter.
- User state for per-user preferences (`UserName`, `FavoriteColor`).
- Redis-backed persistence instead of in-memory state.

## Behavior

| Interaction | Behavior |
|---|---|
| first message in a conversation | Returns `Message #1` and default user prefs |
| more messages in same conversation | Increments conversation counter |
| same user in another conversation | Counter restarts for that conversation, user prefs continue for the user |

## Running the Sample

~~~bash
dotnet run --project samples/StateBot/StateBot.csproj
~~~

In Teams, send multiple messages in one chat, then another chat, to verify conversation vs user state behavior.
