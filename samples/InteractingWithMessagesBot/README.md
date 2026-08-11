# InteractingWithMessagesBot

Demonstrates quoting, threading, and reactions in one bot while keeping each concept
in a separate handler class.

- `QuotingHandlers.cs` - quoted-message metadata and quote composition
- `ThreadingHandlers.cs` - reactive, proactive, and manually constructed threads
- `ReactionHandlers.cs` - reactions on inbound messages and a proactive reaction flow
- `Program.cs` - app setup, handler registration, and help

## Commands

### Quoting

| Command | Behavior |
|---------|----------|
| `quote reply` | `context.ReplyAsync()` auto-quotes the inbound message |
| `quote message` | `context.QuoteAsync()` quotes a previously sent message by ID |
| `quote add` | `AddQuote()` composes a quote with a response |
| `quote batch` | Combines multiple quotes with mixed responses |
| `quote manual` | Combines `AddQuote()` and `AddText()` manually |
| *(quote a message)* | Displays the quoted-message metadata |

### Threading

| Command | Behavior |
|---------|----------|
| `thread reply` | `context.ReplyAsync()` sends a reactive threaded reply |
| `thread send` | `context.SendAsync()` sends to the same thread without quoting |
| `thread proactive` | `teamsApp.ReplyAsync()` sends a proactive threaded reply |
| `thread manual` | `ToThreadedConversationId()` and `teamsApp.SendAsync()` provide manual control |

### Reactions

| Command | Behavior |
|---------|----------|
| `reaction add <type>` | Adds a reaction to the inbound message |
| `reaction remove <type>` | Adds a reaction, then removes it after two seconds |
| `reaction proactive` | Sends a bot message and reacts to it using app-level APIs |
| *(react to a bot message)* | Reports added and removed reactions |

## Run

```bash
dotnet run --project samples/InteractingWithMessagesBot/InteractingWithMessagesBot.csproj
```
