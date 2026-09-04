# InteractingWithMessagesBot

Demonstrates quoting, threading, and reactions in one bot while keeping each concept
in a separate handler class.

- `QuotingHandlers.cs` - quoted-message metadata and quote composition
- `ThreadingHandlers.cs` - default and explicit thread placement
- `ReactionHandlers.cs` - reactions on inbound messages and a proactive reaction flow
- `Program.cs` - app setup, handler registration, and help

## Commands

### Quoting

| Command | Behavior |
|---------|----------|
| `quote reply` | `MessageActivityInput.AddQuote()` quotes the inbound message |
| `quote message` | `MessageActivityInput.AddQuote()` quotes a previously sent message by ID |
| `quote batch` | Combines multiple quotes with mixed responses |
| *(quote a message)* | Displays the quoted-message metadata |

### Threading

| Command | Behavior |
|---------|----------|
| `default send` | `context.SendAsync()` uses the default placement for the current scope without quoting |
| `thread proactive` | `teamsApp.ReplyAsync()` sends a proactive threaded reply |
| `thread proactive quote` | `teamsApp.ReplyAsync()` explicitly places a reply and `AddQuote()` quotes the inbound message |
| `thread proactive targeted` | `teamsApp.ReplyAsync()` sends a proactive targeted reply through the explicit reply endpoint |
| `thread proactive targeted quote` | `teamsApp.ReplyAsync()` sends a proactive targeted reply with an explicit quote |

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
