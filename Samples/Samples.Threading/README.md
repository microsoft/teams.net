# Example: Threading

A bot that demonstrates reactive and proactive threading in Microsoft Teams channels.

## Commands

| Command | Behavior |
|---------|----------|
| `test reply` | `context.Reply()` — reactive threaded reply with visual quote |
| `test send` | `context.Send()` — reactive send to same thread, no quote |
| `test proactive` | `teams.Reply()` — proactive threaded reply |
| `test manual` | `Conversation.ToThreadedConversationId()` + `teams.Send()` — advanced manual control (channels and 1:1 chats only) |
| `help` | Shows available commands |

## Notes

- `test reply` and `test send` work in all scopes (1:1, group chat, channels)
- `test proactive` works in all scopes — in channels it threads, in non-threading scopes it sends normally
- `test manual` only works in channels and 1:1 chats since `ToThreadedConversationId()` constructs a threaded conversation ID (group chats and meetings do not support threading)

## Run

```bash
dotnet run
```

## Configuration

Set credentials in `appsettings.json`:

```json
{
  "Teams": {
    "ClientId": "<your-azure-bot-app-id>",
    "ClientSecret": "<your-azure-bot-app-secret>"
  }
}
```
