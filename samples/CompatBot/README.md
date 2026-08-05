# CompatBot

Demonstrates the Bot Framework compatibility layer on top of the Core SDK. It is the sample to open when you want to move an existing Bot Framework bot onto the Teams Core stack without rewriting the whole app immediately.

## Prerequisites

- Bot Framework app registration and Teams bot credentials.
- A Teams install or channel where the bot can receive messages.

## What it shows

- Basic message handling via `EchoBot`, including the compat adapter and bot registration.
- Teams-specific handlers through the compat layer, so you can keep familiar Bot Framework concepts.
- A proactive `/api/notify/{conversationId}` endpoint that sends a message back into an existing conversation.

---

## Commands / Endpoints

| Route / flow | Behavior |
|-------------|----------|
| `/api/messages` | Receives inbound Teams messages through the compat adapter |
| `/api/notify/{conversationId}` | Sends a proactive message into the provided conversation |
| chat message | Triggers the `EchoBot` and compat middleware |

---
## Running the Sample

~~~bash
dotnet run --project samples/CompatBot/CompatBot.csproj
~~~
In Teams, exercise the commands/flows listed above to validate behavior.

