# StateBot

Shows conversation state and user state in a Teams bot, backed by Redis so values persist across turns.

## Prerequisites

- Bot registered and installed in Teams.
- Redis available and configured through `ConnectionStrings__Redis`.

## Commands

| Command | Behavior |
|---|---|
| `count` | Increments a counter in **conversation** state (shared by everyone in this chat). |
| `my name is <name>` | Saves your name in **user** state. |
| `who am i` | Reads your saved name from user state. |
| `show completed` | Starts a background task that demonstrates sealed state (`IsCompleted`) after turn end. |
| `reset counter` | Clears this conversation's state (counter resets). |
| `help` | Shows command help. |

## Running the Sample

~~~bash
dotnet run --project samples/StateBot/StateBot.csproj
~~~

In Teams, try `count`, `my name is Ada`, `who am i`, `show completed`, and `reset counter` to verify conversation vs user state behavior.
