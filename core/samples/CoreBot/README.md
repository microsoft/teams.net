# CoreBot

Demonstrates `Microsoft.Teams.Core` directly, without the higher-level apps layer. It is the most minimal sample in the repo and shows the raw bot/application wiring with almost no Teams-specific sugar.

## Prerequisites

- A Teams bot registration and endpoint.

## What it shows

- `AddBotApplication()` and `UseBotApplication()` for the bare Core SDK setup.
- `OnActivity` for direct activity handling.
- A reply built with `CoreActivityInput` and sent with `ConversationClient`.

---

## Behavior

| Activity | Behavior |
|---------|----------|
| inbound message | Replies with the SDK version |
| root route | Returns a simple health text |

---

It is useful when you want to understand the raw activity pipeline, or when you need a foundation for a custom integration that does not want the higher-level Teams apps helpers.
## Running the Sample

~~~bash
dotnet run --project samples/CoreBot/CoreBot.csproj
~~~
In Teams, exercise the commands/flows listed above to validate behavior.

