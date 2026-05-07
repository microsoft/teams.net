# Example: Threading

A bot that demonstrates reactive and proactive threading in Microsoft Teams channels.

## Commands

| Command | Behavior |
|---------|----------|
| `test reply` | `context.Reply()` — reactive in-thread reply (sets `ReplyToId`) |
| `test send` | `context.Send()` — reactive send to the same conversation as the inbound activity |
| `test proactive` | `teamsApp.Reply()` — proactive threaded reply via `;messageid=` |
| `test manual` | `Conversation.ToThreadedConversationId()` + `teamsApp.Send()` — advanced manual control |
| `help` | Shows available commands |

## Notes

- `test reply` and `test send` use the inbound conversation reference. Both are visible to the user; `test reply` additionally sets `ReplyToId` so the Teams client renders it as a reply. The visual quote chrome (`<blockquote>` markup) is owned by the upcoming Quoted Replies API and is not part of this sample.
- `test proactive` constructs a threaded conversation ID via `;messageid=<rootId>` and sends to that thread. It exercises the `ToThreadedConversationId` validator (non-zero numeric `messageId`).
- `test manual` does the same as `test proactive` using `Conversation.ToThreadedConversationId()` + `teamsApp.Send()` directly.
- `test proactive` and `test manual` may return a service error in conversation types that do not currently support threading (e.g. meetings, group chats).

## Run

```bash
dotnet run
```
