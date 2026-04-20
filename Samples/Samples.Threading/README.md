# Example: Threading

A bot that demonstrates reactive and proactive threading in Microsoft Teams channels.

## Commands

| Command | Behavior |
|---------|----------|
| `test reply` | `context.Reply()` — reactive threaded reply with visual quote |
| `test send` | `context.Send()` — reactive send to same thread, no quote |
| `test proactive` | `teams.Reply()` — proactive threaded reply |
| `test manual` | `Conversation.ToThreadedConversationId()` + `teams.Send()` — advanced manual control |
| `help` | Shows available commands |

## Notes

- `test reply` and `test send` work in all scopes (1:1, group chat, channels)
- `test proactive` constructs a threaded conversation ID and sends to that thread
- `test manual` does the same using `Conversation.ToThreadedConversationId()` + `teams.Send()` directly
- `test proactive` and `test manual` will return a service error in meetings, which do not currently support threading

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
