# ReactionsBot

Demonstrates message reaction APIs in a focused sample.

## Prerequisites

- Bot registered and installed in Teams.

## What it shows

- Adding reactions to a sent bot message.
- Removing a reaction from the same message.
- Sequenced reaction operations using conversation APIs.

## Commands / Flows

| Input | Behavior |
|---|---|
| `react` | Sends a message, adds two reactions, then removes one reaction |
| `help` | Shows available commands |

## Running the Sample

~~~bash
dotnet run --project samples/ReactionsBot/ReactionsBot.csproj
~~~

In Teams, send `react` and watch the reaction sequence on the bot message.
