# QuotingAndThreadingBot

Demonstrates how a Teams bot can both quote previous messages and work with threaded replies in channels. The sample combines quote parsing, quote creation, reactive replies, and threaded replies in one place.

## What it shows

- Quoted message parsing and response metadata access.
- Multiple quote composition helpers (`Reply`, `Quote`, `AddQuote`).
- Threaded reply patterns using both helper APIs and manual conversation IDs.

## Prerequisites

- Bot registered and installed in a chat or channel
- For the threading commands, use a channel or group chat that supports threaded replies

---

## Commands

| Command | Behavior |
|---------|----------|
| `quote reply` | `Reply()` — auto-quotes the inbound message |
| `quote message` | `Quote()` — sends a message, then quotes it by ID |
| `quote add` | `AddQuote()` — sends a message, then quotes it with the builder helper |
| `quote batch` | Sends three messages, then quotes them with mixed responses |
| `thread send` | `context.SendAsync()` — send to the same conversation |
| `thread reply` | `teamsApp.ReplyAsync()` — threaded reply via `;messageid=` |
| `thread manual` | `ConversationExtensions.ToThreadedConversationId()` + `teamsApp.SendAsync()` |
| *(quote a message)* | Bot reads and displays the quoted reply metadata |

---

## Notes

- The quote handlers show how quoted reply metadata is surfaced back to the bot.
- The threading handlers show the difference between a regular send, a threaded reply, and manually constructing a threaded conversation id.
- `thread reply` and `thread manual` may return a service error in conversation types that do not currently support threading.

---

## Running the Sample

~~~bash
dotnet run --project samples/QuotingAndThreadingBot/QuotingAndThreadingBot.csproj
~~~
In Teams, exercise the commands/flows listed above to validate behavior.
