# InteractingWithMessagesBot

Demonstrates quoting, threading, and reactions in one bot while keeping each concept
in a separate handler class.

- `QuotingHandlers.cs` - quoted-message metadata and quote composition
- `ThreadingHandlers.cs` - reactive and proactive threaded replies, and top-level messages
- `ReactionHandlers.cs` - reactions on inbound messages and a proactive reaction flow
- `Program.cs` - app setup, handler registration, and help

## Reply modes

`context.ReplyAsync()` places and quotes its response according to the reply mode. Set it for the whole app, or pass it on a single call, which overrides the app setting (this sample keeps the default, `Combined`):

```csharp
webAppBuilder.Services.AddTeamsBotApplication(options => options.ReplyMode = ReplyMode.Thread);

await context.ReplyAsync("Hello!", new ReplyOptions { Mode = ReplyMode.Quote });
```

| Mode | In a chat | Quote |
|------|-----------|-------|
| `Combined` (default) | A reply to a top-level message starts a thread under it; a reply inside a thread stays in that thread | Yes |
| `Thread` | Same as `Combined` | No, when the reply lands in a thread |
| `Quote` | The reply stays at the level of the inbound message | Yes |

In a channel every reply goes in the thread of the inbound message, so the mode only decides the quote.

## Commands

### Quoting

| Command | Behavior |
|---------|----------|
| `quote reply` | `context.ReplyAsync(text, new ReplyOptions { Mode = ReplyMode.Quote })` auto-quotes the inbound message and stays at its level (a top-level message gets a top-level reply) |
| `quote message` | `context.QuoteAsync()` quotes a previously sent message by ID |
| `quote add` | `AddQuote()` composes a quote with a response |
| `quote batch` | Combines multiple quotes with mixed responses |
| `quote manual` | Combines `AddQuote()` and `AddText()` manually |
| *(quote a message)* | Displays the quoted-message metadata |

### Threading

| Command | Behavior |
|---------|----------|
| `thread reply` | `context.ReplyAsync()` with the default reply mode, `Combined`. In a chat, a reply to a top-level message starts a thread under it, and a reply inside a thread stays in that thread. The inbound message is quoted. In a channel it is sent in the same thread |
| `thread reply unquoted` | `context.ReplyAsync(text, new ReplyOptions { Mode = ReplyMode.Thread })`. The same placement as `Combined`, without the quote when the reply lands in a thread |
| `thread send` | `context.SendAsync()` sends reactively without quoting. In a channel it is sent in the same thread. In a chat it matches the level of your message (in the thread if your message is in one, otherwise top-level). In a chat, unlike `ReplyAsync`, `SendAsync` never starts a new thread |
| `thread proactive` | `teamsApp.ReplyAsync()` replies in the reply chain (thread) of your message without the inbound activity, as a bot does when it replies later. It never quotes. It uses `ChannelData.Chat.Id` (or `ChannelData.Channel.Id`) and `ChannelData.ReplyChain.Id`. Without `ReplyChain.Id` the sample answers that there is not enough thread metadata |
| `thread top-level` | `teamsApp.SendAsync()` to `ChannelData.Chat.Id` (or `ChannelData.Channel.Id`) sends a new top-level message |

### Reactions

| Command | Behavior |
|---------|----------|
| `reaction add <type>` | Adds a reaction to the inbound message |
| `reaction remove <type>` | Adds a reaction, then removes it after two seconds |
| `reaction proactive` | Sends a bot message and reacts to it using app-level APIs |
| *(react to a bot message)* | Reports added and removed reactions with `context.ReplyAsync()` (in a chat, in a thread under the message you reacted to) |

## Notes

- Threading behavior depends on the conversation type. A threaded reply, from `context.ReplyAsync()` or a proactive `teamsApp.ReplyAsync()`, may return a service error where threading is unsupported.

## Run

```bash
dotnet run --project samples/InteractingWithMessagesBot/InteractingWithMessagesBot.csproj
```
